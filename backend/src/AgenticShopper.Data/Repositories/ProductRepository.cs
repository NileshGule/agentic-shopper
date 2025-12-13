using AgenticShopper.Core.Interfaces;
using AgenticShopper.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AgenticShopper.Data.Repositories;

public class ProductRepository : IRepository<Product>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(ApplicationDbContext context, ILogger<ProductRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {ProductId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all products");
            throw;
        }
    }

    public async Task<Product> AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            entity.CreatedDate = DateTime.UtcNow;

            await _context.Products.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added new product with ID {ProductId}: {ProductName}", entity.Id, entity.Name);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product {ProductName}", entity.Name);
            throw;
        }
    }

    public async Task<Product> UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            _context.Products.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated product with ID {ProductId}: {ProductName}", entity.Id, entity.Name);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID {ProductId}", entity.Id);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> FindAsync(
        Expression<Func<Product, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding products");
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _context.Products.FindAsync([id], cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
                throw new InvalidOperationException($"Product with ID {id} not found");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted product with ID {ProductId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
            throw;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Products.AnyAsync(p => p.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of product with ID {ProductId}", id);
            throw;
        }
    }

    // Product-specific methods
    public async Task<IEnumerable<Product>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for category {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<Product>();

        try
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => EF.Functions.Like(p.Name, $"%{searchTerm}%"))
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with term {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<Product?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        try
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product by name {ProductName}", name);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetUncategorizedProductsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Products
                .Where(p => p.CategoryId == null || p.Category == null)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving uncategorized products");
            throw;
        }
    }

    /// <summary>
    /// Generate normalized product name for matching (lowercase, remove special chars, trim)
    /// </summary>
    public static string GenerateNormalizedName(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return string.Empty;

        // Convert to lowercase
        var normalized = productName.ToLowerInvariant();

        // Remove common brand suffixes and packaging info
        var commonSuffixes = new[] { " pk", " pack", " kg", " g", " ml", " l", " ea", " each" };
        foreach (var suffix in commonSuffixes)
        {
            if (normalized.EndsWith(suffix))
            {
                normalized = normalized.Substring(0, normalized.Length - suffix.Length);
            }
        }

        // Remove special characters except spaces
        normalized = new string(normalized.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());

        // Replace multiple spaces with single space and trim
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized.Trim();
    }

    /// <summary>
    /// Find product by normalized name match
    /// </summary>
    public async Task<Product?> GetByNormalizedNameAsync(
        string productName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return null;

        try
        {
            var normalizedSearch = GenerateNormalizedName(productName);
            
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync(cancellationToken);

            return products.FirstOrDefault(p => 
                GenerateNormalizedName(p.Name) == normalizedSearch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product by normalized name {ProductName}", productName);
            throw;
        }
    }

    /// <summary>
    /// Update product notes and tags
    /// </summary>
    public async Task<Product> SaveNotesAndTagsAsync(
        Guid productId,
        string? notes,
        string? tags,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _context.Products.FindAsync([productId], cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for notes/tags update", productId);
                throw new InvalidOperationException($"Product with ID {productId} not found");
            }

            product.Notes = notes?.Trim();
            product.Tags = tags;

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated notes/tags for product {ProductId}", productId);

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notes/tags for product {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Search products by note content
    /// </summary>
    public async Task<IEnumerable<Product>> SearchByNotesAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<Product>();

        try
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Notes != null && EF.Functions.Like(p.Notes, $"%{searchTerm}%"))
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products by notes with term {SearchTerm}", searchTerm);
            throw;
        }
    }

    /// <summary>
    /// Get products by tags (matches any tag in the provided list)
    /// </summary>
    public async Task<IEnumerable<Product>> GetByTagsAsync(
        string[] tags,
        CancellationToken cancellationToken = default)
    {
        if (tags == null || tags.Length == 0)
            return Enumerable.Empty<Product>();

        try
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Tags != null)
                .ToListAsync(cancellationToken);

            // Filter in-memory for JSON array matching
            return products.Where(p =>
            {
                if (string.IsNullOrWhiteSpace(p.Tags))
                    return false;

                try
                {
                    var productTags = System.Text.Json.JsonSerializer.Deserialize<string[]>(p.Tags);
                    if (productTags == null)
                        return false;

                    return tags.Any(tag => productTags.Contains(tag, StringComparer.OrdinalIgnoreCase));
                }
                catch
                {
                    return false;
                }
            }).OrderBy(p => p.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by tags");
            throw;
        }
    }

    /// <summary>
    /// Get all unique tags across all products
    /// </summary>
    public async Task<IEnumerable<string>> GetAllTagsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var products = await _context.Products
                .Where(p => p.Tags != null)
                .Select(p => p.Tags)
                .ToListAsync(cancellationToken);

            var allTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tagsJson in products)
            {
                if (string.IsNullOrWhiteSpace(tagsJson))
                    continue;

                try
                {
                    var tags = System.Text.Json.JsonSerializer.Deserialize<string[]>(tagsJson);
                    if (tags != null)
                    {
                        foreach (var tag in tags)
                        {
                            allTags.Add(tag);
                        }
                    }
                }
                catch
                {
                    // Skip invalid JSON
                    continue;
                }
            }

            return allTags.OrderBy(t => t);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tags");
            throw;
        }
    }
}
