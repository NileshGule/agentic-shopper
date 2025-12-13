using AgenticShopper.Agents.PriceComparison.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgenticShopper.Agents.PriceComparison.Controllers;

/// <summary>
/// Controller for price comparison and promotion operations
/// </summary>
[ApiController]
[Route("api/v1")]
public class PriceController : ControllerBase
{
    private readonly PriceAgent _priceAgent;
    private readonly PriceOptimizer _priceOptimizer;
    private readonly ILogger<PriceController> _logger;

    public PriceController(
        PriceAgent priceAgent,
        PriceOptimizer priceOptimizer,
        ILogger<PriceController> logger)
    {
        _priceAgent = priceAgent;
        _priceOptimizer = priceOptimizer;
        _logger = logger;
    }

    /// <summary>
    /// Get current promotions with optional filtering
    /// </summary>
    /// <param name="store">Filter by store (Coles, Woolworths, All)</param>
    /// <param name="productName">Search by product name</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 50)</param>
    [HttpGet("promotions/current")]
    [ProducesResponseType(typeof(CurrentPromotionsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentPromotions(
        [FromQuery] string? store = null,
        [FromQuery] string? productName = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting current promotions: store={Store}, product={Product}, page={Page}",
                store ?? "All", productName ?? "All", page);

            var request = new CurrentPromotionsRequest
            {
                StoreName = store == "All" ? null : store,
                ProductName = productName,
                Page = page,
                PageSize = pageSize
            };

            var result = await _priceAgent.Execute<CurrentPromotionsRequest, CurrentPromotionsResponse>(
                request, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to get promotions: {Error}", result.ErrorMessage);
                return StatusCode(500, new { error = result.ErrorMessage });
            }

            var response = new CurrentPromotionsResponseDto
            {
                Promotions = result.Data.Promotions.Select(p => new PromotionDto
                {
                    Id = p.Id,
                    ProductName = p.ProductName,
                    StoreName = p.StoreName,
                    OriginalPrice = p.OriginalPrice,
                    SalePrice = p.SalePrice,
                    DiscountPercentage = p.DiscountPercentage,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    CatalogWeek = p.CatalogWeek
                }).ToList(),
                Pagination = new PaginationDto
                {
                    Page = result.Data.Pagination.Page,
                    PageSize = result.Data.Pagination.PageSize,
                    TotalItems = result.Data.Pagination.TotalCount,
                    TotalPages = result.Data.Pagination.TotalPages
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current promotions");
            return StatusCode(500, new { error = "Failed to retrieve promotions" });
        }
    }

    /// <summary>
    /// Compare prices across stores for shopping list items
    /// </summary>
    /// <param name="request">List of products to compare</param>
    [HttpPost("prices/compare")]
    [ProducesResponseType(typeof(PriceComparisonResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ComparePrices(
        [FromBody] PriceComparisonRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Products == null || !request.Products.Any())
            {
                return BadRequest(new { error = "Products list cannot be empty" });
            }

            _logger.LogInformation("Comparing prices for {Count} products", request.Products.Count);

            var agentRequest = new PriceComparisonRequest
            {
                Products = request.Products.Select(p => new ProductPriceQuery
                {
                    ProductName = p.ProductName,
                    Quantity = p.Quantity
                }).ToList()
            };

            var result = await _priceAgent.Execute<PriceComparisonRequest, PriceComparisonResponse>(
                agentRequest, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to compare prices: {Error}", result.ErrorMessage);
                return StatusCode(500, new { error = result.ErrorMessage });
            }

            var response = new PriceComparisonResponseDto
            {
                Items = result.Data.Items.Select(item => new ProductPriceComparisonDto
                {
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    ColesPrice = item.ColesPrice,
                    WoolworthsPrice = item.WoolworthsPrice,
                    ColesSalePrice = item.ColesSalePrice,
                    WoolworthsSalePrice = item.WoolworthsSalePrice,
                    HasColesPromotion = item.HasColesPromotion,
                    HasWoolworthsPromotion = item.HasWoolworthsPromotion,
                    BestPrice = item.ColesSalePrice ?? item.ColesPrice ?? item.WoolworthsSalePrice ?? item.WoolworthsPrice,
                    BestStore = item.RecommendedStore,
                    Savings = item.PotentialSavings
                }).ToList(),
                Summary = new PricingSummaryDto
                {
                    ColesTotal = result.Data.Summary.ColesTotal,
                    WoolworthsTotal = result.Data.Summary.WoolworthsTotal,
                    PotentialSavings = result.Data.Summary.PotentialSavings,
                    RecommendedStore = result.Data.Summary.RecommendedStore,
                    SplitStrategy = result.Data.Summary.SplitStrategy != null ? new SplitStrategyDto
                    {
                        ColesPurchases = result.Data.Summary.SplitStrategy.ColesPurchases,
                        WoolworthsPurchases = result.Data.Summary.SplitStrategy.WoolworthsPurchases,
                        TotalSavings = result.Data.Summary.SplitStrategy.TotalSavings
                    } : null
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing prices");
            return StatusCode(500, new { error = "Failed to compare prices" });
        }
    }

    /// <summary>
    /// Trigger manual promotion data refresh (admin only)
    /// </summary>
    [HttpPost("promotions/refresh")]
    [ProducesResponseType(typeof(RefreshJobResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshPromotions(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting manual promotion refresh");

            var result = await _priceAgent.RefreshPromotionsAsync(cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to refresh promotions: {Error}", result.ErrorMessage);
                return StatusCode(500, new { error = result.ErrorMessage });
            }

            var jobId = Guid.NewGuid().ToString();
            var response = new RefreshJobResponseDto
            {
                JobId = jobId,
                Status = "Started",
                PromotionsAdded = result.Data
            };

            return StatusCode(202, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing promotions");
            return StatusCode(500, new { error = "Failed to refresh promotions" });
        }
    }
}

#region DTOs

public class PriceComparisonRequestDto
{
    public List<ProductQueryDto> Products { get; set; } = new();
}

public class ProductQueryDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
}

public class CurrentPromotionsResponseDto
{
    public List<PromotionDto> Promotions { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
}

public class PromotionDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string CatalogWeek { get; set; } = string.Empty;
}

public class PaginationDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public class PriceComparisonResponseDto
{
    public List<ProductPriceComparisonDto> Items { get; set; } = new();
    public PricingSummaryDto Summary { get; set; } = new();
}

public class ProductPriceComparisonDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? ColesPrice { get; set; }
    public decimal? WoolworthsPrice { get; set; }
    public decimal? ColesSalePrice { get; set; }
    public decimal? WoolworthsSalePrice { get; set; }
    public bool HasColesPromotion { get; set; }
    public bool HasWoolworthsPromotion { get; set; }
    public decimal? BestPrice { get; set; }
    public string BestStore { get; set; } = "Unknown";
    public decimal Savings { get; set; }
}

public class PricingSummaryDto
{
    public decimal ColesTotal { get; set; }
    public decimal WoolworthsTotal { get; set; }
    public decimal PotentialSavings { get; set; }
    public string RecommendedStore { get; set; } = "Unknown";
    public SplitStrategyDto? SplitStrategy { get; set; }
}

public class SplitStrategyDto
{
    public List<string> ColesPurchases { get; set; } = new();
    public List<string> WoolworthsPurchases { get; set; } = new();
    public decimal TotalSavings { get; set; }
}

public class RefreshJobResponseDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PromotionsAdded { get; set; }
}

#endregion
