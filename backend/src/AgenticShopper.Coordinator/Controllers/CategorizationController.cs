using AgenticShopper.Agents.Categorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenticShopper.Coordinator.Controllers;

/// <summary>
/// DTOs for categorization API
/// </summary>
public class SuggestCategoryRequest
{
    public required string ProductName { get; set; }
    public string? StoreName { get; set; }
}

public class AssignCategoryRequest
{
    public required Guid ProductId { get; set; }
    public required Guid CategoryId { get; set; }
}

public class BatchCategorizationDto
{
    public required List<Guid> ProductIds { get; set; }
    public bool ForceRecategorize { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCustom { get; set; }
}

/// <summary>
/// API controller for product categorization operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategorizationController : ControllerBase
{
    private readonly CategorizationAgent _agent;
    private readonly ILogger<CategorizationController> _logger;

    public CategorizationController(
        CategorizationAgent agent,
        ILogger<CategorizationController> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    /// <summary>
    /// Suggest a category for a product name (without saving)
    /// </summary>
    [HttpPost("suggest")]
    public async Task<ActionResult<CategorizationResponse>> SuggestCategory(
        [FromBody] SuggestCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Suggesting category for product: {ProductName}", request.ProductName);

            // Create a temporary categorization request
            var categorizationRequest = new CategorizationRequest
            {
                ProductId = Guid.Empty, // Not saving, just suggesting
                ProductName = request.ProductName,
                StoreName = request.StoreName,
                ForceRecategorize = false
            };

            var result = await _agent.ExecuteAsync<CategorizationRequest, CategorizationResponse>(
                categorizationRequest,
                cancellationToken
            );

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting category");
            return StatusCode(500, new { error = "Failed to suggest category" });
        }
    }

    /// <summary>
    /// Automatically categorize a single product
    /// </summary>
    [HttpPost("categorize/{productId}")]
    public async Task<ActionResult<CategorizationResponse>> CategorizeProduct(
        Guid productId,
        [FromQuery] bool forceRecategorize = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Categorizing product {ProductId}", productId);

            var request = new CategorizationRequest
            {
                ProductId = productId,
                ProductName = string.Empty, // Will be loaded from database
                ForceRecategorize = forceRecategorize
            };

            var result = await _agent.ExecuteAsync<CategorizationRequest, CategorizationResponse>(
                request,
                cancellationToken
            );

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error categorizing product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to categorize product" });
        }
    }

    /// <summary>
    /// Batch categorize multiple products
    /// </summary>
    [HttpPost("categorize/batch")]
    public async Task<ActionResult<List<CategorizationResponse>>> CategorizeBatch(
        [FromBody] BatchCategorizationDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Batch categorizing {Count} products", request.ProductIds.Count);

            var batchRequest = new BatchCategorizationRequest
            {
                ProductIds = request.ProductIds,
                ForceRecategorize = request.ForceRecategorize
            };

            var results = await _agent.CategorizeBatchProductsAsync(batchRequest, cancellationToken);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch categorization");
            return StatusCode(500, new { error = "Failed to categorize products" });
        }
    }

    /// <summary>
    /// Manually assign a category to a product (FR-009)
    /// </summary>
    [HttpPost("assign")]
    public async Task<ActionResult<CategorizationResponse>> AssignCategory(
        [FromBody] AssignCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Manually assigning category {CategoryId} to product {ProductId}",
                request.CategoryId, request.ProductId);

            var result = await _agent.ManualOverrideAsync(
                request.ProductId,
                request.CategoryId,
                cancellationToken
            );

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning category");
            return StatusCode(500, new { error = "Failed to assign category" });
        }
    }

    /// <summary>
    /// Get all available categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories(
        CancellationToken cancellationToken)
    {
        try
        {
            // This would typically come from a repository
            // For now, return predefined categories
            var categories = new List<CategoryDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Fruits & Vegetables", IsCustom = false },
                new() { Id = Guid.NewGuid(), Name = "Meat & Seafood", IsCustom = false },
                new() { Id = Guid.NewGuid(), Name = "Dairy & Eggs", IsCustom = false },
                new() { Id = Guid.NewGuid(), Name = "Bakery & Bread", IsCustom = false },
                new() { Id = Guid.NewGuid(), Name = "Pantry Staples", IsCustom = false },
                new() { Id = Guid.NewGuid(), Name = "Snacks & Sweets", IsCustom = false },
                new() { Id = Guid.NewGuid(), Name = "Beverages", IsCustom = false },
                new() { Id = Guid.NewGuid(), Name = "Frozen Foods", IsCustom = false },
                new() { Id = Guid.NewGuid(), Name = "Household & Cleaning", IsCustom = false },
                new() { Id = Guid.NewGuid(), Name = "Personal Care", IsCustom = false },
                new() { Id = Guid.NewGuid(), Name = "Other", IsCustom = false }
            };

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return StatusCode(500, new { error = "Failed to get categories" });
        }
    }
}
