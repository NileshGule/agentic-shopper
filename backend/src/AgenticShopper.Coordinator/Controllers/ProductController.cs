using AgenticShopper.Data.Repositories;
using AgenticShopper.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AgenticShopper.Coordinator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ILogger<ProductController> _logger;
    private readonly ProductRepository _productRepository;

    public ProductController(
        ILogger<ProductController> logger,
        ProductRepository productRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null)
            {
                return NotFound(new { error = $"Product with ID {id} not found" });
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the product" });
        }
    }

    /// <summary>
    /// Get all products
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts(CancellationToken cancellationToken)
    {
        try
        {
            var products = await _productRepository.GetAllAsync(cancellationToken);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all products");
            return StatusCode(500, new { error = "An error occurred while retrieving products" });
        }
    }

    /// <summary>
    /// Search products by name
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<Product>>> SearchProducts(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Search query is required" });
        }

        try
        {
            var products = await _productRepository.SearchByNameAsync(query, cancellationToken);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with query {Query}", query);
            return StatusCode(500, new { error = "An error occurred while searching products" });
        }
    }

    /// <summary>
    /// Update product notes and tags
    /// </summary>
    [HttpPut("{id}/notes-tags")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> UpdateNotesAndTags(
        Guid id,
        [FromBody] UpdateNotesTagsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate tags JSON if provided
            string? tagsJson = null;
            if (request.Tags != null && request.Tags.Length > 0)
            {
                tagsJson = JsonSerializer.Serialize(request.Tags);
            }

            var product = await _productRepository.SaveNotesAndTagsAsync(
                id,
                request.Notes,
                tagsJson,
                cancellationToken);

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Product {ProductId} not found for notes/tags update", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notes/tags for product {ProductId}", id);
            return StatusCode(500, new { error = "An error occurred while updating product notes/tags" });
        }
    }

    /// <summary>
    /// Search products by note content
    /// </summary>
    [HttpGet("search-by-notes")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<Product>>> SearchByNotes(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Search query is required" });
        }

        try
        {
            var products = await _productRepository.SearchByNotesAsync(query, cancellationToken);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products by notes with query {Query}", query);
            return StatusCode(500, new { error = "An error occurred while searching products by notes" });
        }
    }

    /// <summary>
    /// Get products by tags
    /// </summary>
    [HttpPost("filter-by-tags")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<Product>>> FilterByTags(
        [FromBody] FilterByTagsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Tags == null || request.Tags.Length == 0)
        {
            return BadRequest(new { error = "At least one tag is required" });
        }

        try
        {
            var products = await _productRepository.GetByTagsAsync(request.Tags, cancellationToken);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering products by tags");
            return StatusCode(500, new { error = "An error occurred while filtering products by tags" });
        }
    }

    /// <summary>
    /// Get all unique tags
    /// </summary>
    [HttpGet("tags")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetAllTags(CancellationToken cancellationToken)
    {
        try
        {
            var tags = await _productRepository.GetAllTagsAsync(cancellationToken);
            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tags");
            return StatusCode(500, new { error = "An error occurred while retrieving tags" });
        }
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    [HttpGet("category/{categoryId}")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        try
        {
            var products = await _productRepository.GetByCategoryAsync(categoryId, cancellationToken);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for category {CategoryId}", categoryId);
            return StatusCode(500, new { error = "An error occurred while retrieving products by category" });
        }
    }
}

public record UpdateNotesTagsRequest(string? Notes, string[]? Tags);
public record FilterByTagsRequest(string[] Tags);
