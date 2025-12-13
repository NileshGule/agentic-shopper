using AgenticShopper.Agents.PriceComparison.Interfaces;
using AgenticShopper.Core.Abstractions;
using AgenticShopper.Core.Models;
using AgenticShopper.Data;
using AgenticShopper.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.PriceComparison;

/// <summary>
/// Request for price comparison
/// </summary>
public class PriceComparisonRequest
{
    public List<ProductPriceQuery> Products { get; set; } = new();
}

public class ProductPriceQuery
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
}

/// <summary>
/// Request for current promotions
/// </summary>
public class CurrentPromotionsRequest
{
    public string? StoreName { get; set; }
    public string? ProductName { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Result of price comparison
/// </summary>
public class PriceComparisonResponse
{
    public List<ProductPriceComparison> Items { get; set; } = new();
    public PricingSummary Summary { get; set; } = new();
}

public class ProductPriceComparison
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? ColesPrice { get; set; }
    public decimal? WoolworthsPrice { get; set; }
    public decimal? ColesSalePrice { get; set; }
    public decimal? WoolworthsSalePrice { get; set; }
    public bool HasColesPromotion { get; set; }
    public bool HasWoolworthsPromotion { get; set; }
    public string RecommendedStore { get; set; } = "Unknown";
    public decimal PotentialSavings { get; set; }
}

public class PricingSummary
{
    public decimal ColesTotal { get; set; }
    public decimal WoolworthsTotal { get; set; }
    public decimal PotentialSavings { get; set; }
    public string RecommendedStore { get; set; } = "Unknown";
    public SplitStrategy? SplitStrategy { get; set; }
}

public class SplitStrategy
{
    public List<string> ColesPurchases { get; set; } = new();
    public List<string> WoolworthsPurchases { get; set; } = new();
    public decimal TotalSavings { get; set; }
}

/// <summary>
/// Response for current promotions
/// </summary>
public class CurrentPromotionsResponse
{
    public List<Promotion> Promotions { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Agent responsible for price comparison and promotion tracking
/// </summary>
public class PriceAgent : AgentBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly PromotionRepository _promotionRepository;
    private readonly IEnumerable<IStoreCatalogService> _catalogServices;

    public override string AgentId => "price-agent";
    public override string AgentName => "Price Comparison Agent";
    public override string Description => "Compares prices across stores and tracks promotions";

    public PriceAgent(
        ApplicationDbContext dbContext,
        PromotionRepository promotionRepository,
        IEnumerable<IStoreCatalogService> catalogServices,
        ILogger<PriceAgent> logger,
        IChatClient? chatClient = null)
        : base(logger, chatClient)
    {
        _dbContext = dbContext;
        _promotionRepository = promotionRepository;
        _catalogServices = catalogServices;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Initializing PriceAgent");
        await base.InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// Execute price comparison or promotion queries
    /// </summary>
    protected override async Task<AgentResult<TOutput>> OnExecuteAsync<TInput, TOutput>(
        TInput input,
        CancellationToken cancellationToken = default)
    {
        if (input is PriceComparisonRequest priceRequest && typeof(TOutput) == typeof(PriceComparisonResponse))
        {
            var result = await ComparePricesAsync(priceRequest, cancellationToken);
            return AgentResult<TOutput>.Success((TOutput)(object)result);
        }

        if (input is CurrentPromotionsRequest promotionsRequest && typeof(TOutput) == typeof(CurrentPromotionsResponse))
        {
            var result = await GetCurrentPromotionsAsync(promotionsRequest, cancellationToken);
            return AgentResult<TOutput>.Success((TOutput)(object)result);
        }

        return AgentResult<TOutput>.Failure($"Unsupported request type: {typeof(TInput).Name}");
    }

    /// <summary>
    /// Compare prices across stores for shopping list items
    /// </summary>
    private async Task<PriceComparisonResponse> ComparePricesAsync(
        PriceComparisonRequest request,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Comparing prices for {Count} products", request.Products.Count);

        var response = new PriceComparisonResponse();
        decimal colesTotal = 0;
        decimal woolworthsTotal = 0;

        foreach (var product in request.Products)
        {
            var comparison = await CompareProductPriceAsync(product, cancellationToken);
            response.Items.Add(comparison);

            // Calculate totals (use sale price if available, otherwise regular price)
            if (comparison.ColesPrice.HasValue || comparison.ColesSalePrice.HasValue)
            {
                var colesPrice = comparison.ColesSalePrice ?? comparison.ColesPrice ?? 0;
                colesTotal += colesPrice * comparison.Quantity;
            }

            if (comparison.WoolworthsPrice.HasValue || comparison.WoolworthsSalePrice.HasValue)
            {
                var woolworthsPrice = comparison.WoolworthsSalePrice ?? comparison.WoolworthsPrice ?? 0;
                woolworthsTotal += woolworthsPrice * comparison.Quantity;
            }
        }

        response.Summary.ColesTotal = colesTotal;
        response.Summary.WoolworthsTotal = woolworthsTotal;
        response.Summary.PotentialSavings = Math.Abs(colesTotal - woolworthsTotal);
        response.Summary.RecommendedStore = colesTotal < woolworthsTotal ? "Coles" :
                                            woolworthsTotal < colesTotal ? "Woolworths" : "Equal";

        // Calculate split strategy for maximum savings
        response.Summary.SplitStrategy = CalculateSplitStrategy(response.Items);

        Logger.LogInformation(
            "Price comparison complete: Coles ${ColesTotal:F2}, Woolworths ${WoolworthsTotal:F2}, Savings ${Savings:F2}",
            colesTotal, woolworthsTotal, response.Summary.PotentialSavings);

        return response;
    }

    /// <summary>
    /// Compare price for a single product
    /// </summary>
    private async Task<ProductPriceComparison> CompareProductPriceAsync(
        ProductPriceQuery query,
        CancellationToken cancellationToken)
    {
        var comparison = new ProductPriceComparison
        {
            ProductName = query.ProductName,
            Quantity = query.Quantity
        };

        // Search for promotions at both stores
        var promotions = await _promotionRepository.SearchByProductNameAsync(
            query.ProductName,
            activeOnly: true,
            cancellationToken);

        foreach (var promotion in promotions)
        {
            if (promotion.StoreName == "Coles")
            {
                comparison.ColesPrice = promotion.OriginalPrice;
                comparison.ColesSalePrice = promotion.SalePrice;
                comparison.HasColesPromotion = true;
            }
            else if (promotion.StoreName == "Woolworths")
            {
                comparison.WoolworthsPrice = promotion.OriginalPrice;
                comparison.WoolworthsSalePrice = promotion.SalePrice;
                comparison.HasWoolworthsPromotion = true;
            }
        }

        // Determine recommended store for this product
        var colesEffectivePrice = comparison.ColesSalePrice ?? comparison.ColesPrice;
        var woolworthsEffectivePrice = comparison.WoolworthsSalePrice ?? comparison.WoolworthsPrice;

        if (colesEffectivePrice.HasValue && woolworthsEffectivePrice.HasValue)
        {
            comparison.RecommendedStore = colesEffectivePrice < woolworthsEffectivePrice ? "Coles" : "Woolworths";
            comparison.PotentialSavings = Math.Abs(colesEffectivePrice.Value - woolworthsEffectivePrice.Value) * query.Quantity;
        }
        else if (colesEffectivePrice.HasValue)
        {
            comparison.RecommendedStore = "Coles";
        }
        else if (woolworthsEffectivePrice.HasValue)
        {
            comparison.RecommendedStore = "Woolworths";
        }

        return comparison;
    }

    /// <summary>
    /// Calculate optimal split strategy between stores
    /// </summary>
    private SplitStrategy CalculateSplitStrategy(List<ProductPriceComparison> items)
    {
        var strategy = new SplitStrategy();
        decimal totalSavings = 0;

        foreach (var item in items)
        {
            if (item.RecommendedStore == "Coles")
            {
                strategy.ColesPurchases.Add(item.ProductName);
                totalSavings += item.PotentialSavings;
            }
            else if (item.RecommendedStore == "Woolworths")
            {
                strategy.WoolworthsPurchases.Add(item.ProductName);
                totalSavings += item.PotentialSavings;
            }
        }

        strategy.TotalSavings = totalSavings;
        return strategy;
    }

    /// <summary>
    /// Get current promotions with filtering
    /// </summary>
    private async Task<CurrentPromotionsResponse> GetCurrentPromotionsAsync(
        CurrentPromotionsRequest request,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Fetching current promotions for store: {Store}, product: {Product}",
            request.StoreName ?? "All", request.ProductName ?? "All");

        IEnumerable<Promotion> promotions;

        if (!string.IsNullOrEmpty(request.ProductName))
        {
            promotions = await _promotionRepository.SearchByProductNameAsync(
                request.ProductName,
                activeOnly: true,
                cancellationToken);
        }
        else if (!string.IsNullOrEmpty(request.StoreName))
        {
            promotions = await _promotionRepository.GetByStoreAsync(
                request.StoreName,
                activeOnly: true,
                cancellationToken);
        }
        else
        {
            promotions = await _promotionRepository.GetActivePromotionsAsync(cancellationToken);
        }

        var promotionsList = promotions.ToList();
        var totalCount = promotionsList.Count;

        // Apply pagination
        var paginatedPromotions = promotionsList
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new CurrentPromotionsResponse
        {
            Promotions = paginatedPromotions,
            Pagination = new PaginationInfo
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            }
        };
    }

    /// <summary>
    /// Refresh promotions from all catalog services
    /// </summary>
    public async Task<AgentResult<int>> RefreshPromotionsAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting promotion refresh from all store catalogs");

        try
        {
            var allPromotions = new List<Promotion>();

            foreach (var catalogService in _catalogServices)
            {
                try
                {
                    var promotions = await catalogService.FetchPromotionsAsync(cancellationToken);
                    allPromotions.AddRange(promotions);

                    Logger.LogInformation("Fetched {Count} promotions from {Store}",
                        promotions.Count(), catalogService.StoreName);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to fetch promotions from {Store}", catalogService.StoreName);
                }
            }

            // Delete expired promotions first
            var expiredCount = await _promotionRepository.DeleteExpiredPromotionsAsync(cancellationToken);
            Logger.LogInformation("Deleted {Count} expired promotions", expiredCount);

            // Bulk insert new promotions
            var insertedCount = await _promotionRepository.BulkAddAsync(allPromotions, cancellationToken);

            Logger.LogInformation("Promotion refresh complete: {Count} promotions added", insertedCount);
            return AgentResult<int>.Success(insertedCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during promotion refresh");
            return AgentResult<int>.Failure($"Promotion refresh failed: {ex.Message}");
        }
    }

    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Shutting down PriceAgent");
        await base.ShutdownAsync(cancellationToken);
    }
}
