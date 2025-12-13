using AgenticShopper.Core.Interfaces;
using AgenticShopper.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AgenticShopper.Data.Repositories;

/// <summary>
/// Repository for managing promotions from store catalogs
/// </summary>
public class PromotionRepository : IRepository<Promotion>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PromotionRepository> _logger;

    public PromotionRepository(ApplicationDbContext context, ILogger<PromotionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promotion with ID {PromotionId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Promotion>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Promotions
                .OrderBy(p => p.ProductName)
                .ThenBy(p => p.StoreName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all promotions");
            throw;
        }
    }

    public async Task<Promotion> AddAsync(Promotion entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            entity.LastUpdated = DateTime.UtcNow;

            await _context.Promotions.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added new promotion for {ProductName} at {StoreName}", 
                entity.ProductName, entity.StoreName);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding promotion for {ProductName}", entity.ProductName);
            throw;
        }
    }

    public async Task<Promotion> UpdateAsync(Promotion entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            entity.LastUpdated = DateTime.UtcNow;
            _context.Promotions.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated promotion for {ProductName} at {StoreName}", 
                entity.ProductName, entity.StoreName);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating promotion with ID {PromotionId}", entity.Id);
            throw;
        }
    }

    public async Task<IEnumerable<Promotion>> FindAsync(
        Expression<Func<Promotion, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Promotions
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding promotions with predicate");
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var promotion = await GetByIdAsync(id, cancellationToken);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted promotion with ID {PromotionId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting promotion with ID {PromotionId}", id);
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
            return await _context.Promotions.AnyAsync(p => p.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of promotion with ID {PromotionId}", id);
            throw;
        }
    }

    /// <summary>
    /// Get active promotions (not expired)
    /// </summary>
    public async Task<IEnumerable<Promotion>> GetActivePromotionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.Promotions
                .Where(p => p.EndDate >= now)
                .OrderBy(p => p.StoreName)
                .ThenBy(p => p.ProductName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active promotions");
            throw;
        }
    }

    /// <summary>
    /// Get promotions for a specific store
    /// </summary>
    public async Task<IEnumerable<Promotion>> GetByStoreAsync(
        string storeName,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Promotions.Where(p => p.StoreName == storeName);

            if (activeOnly)
            {
                var now = DateTime.UtcNow;
                query = query.Where(p => p.EndDate >= now);
            }

            return await query
                .OrderBy(p => p.ProductName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promotions for store {StoreName}", storeName);
            throw;
        }
    }

    /// <summary>
    /// Search promotions by product name (fuzzy matching using normalized name)
    /// </summary>
    public async Task<IEnumerable<Promotion>> SearchByProductNameAsync(
        string productName,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedSearch = productName.ToLowerInvariant().Trim();

            var query = _context.Promotions
                .Where(p => p.NormalizedProductName.Contains(normalizedSearch));

            if (activeOnly)
            {
                var now = DateTime.UtcNow;
                query = query.Where(p => p.EndDate >= now);
            }

            return await query
                .OrderBy(p => p.StoreName)
                .ThenBy(p => p.ProductName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching promotions by product name {ProductName}", productName);
            throw;
        }
    }

    /// <summary>
    /// Get promotions by catalog week (e.g., "2025-W50")
    /// </summary>
    public async Task<IEnumerable<Promotion>> GetByCatalogWeekAsync(
        string catalogWeek,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Promotions
                .Where(p => p.CatalogWeek == catalogWeek)
                .OrderBy(p => p.StoreName)
                .ThenBy(p => p.ProductName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promotions for catalog week {CatalogWeek}", catalogWeek);
            throw;
        }
    }

    /// <summary>
    /// Delete expired promotions (cleanup job)
    /// </summary>
    public async Task<int> DeleteExpiredPromotionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredPromotions = await _context.Promotions
                .Where(p => p.EndDate < now)
                .ToListAsync(cancellationToken);

            if (expiredPromotions.Any())
            {
                _context.Promotions.RemoveRange(expiredPromotions);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted {Count} expired promotions", expiredPromotions.Count);
            }

            return expiredPromotions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expired promotions");
            throw;
        }
    }

    /// <summary>
    /// Bulk insert promotions (for weekly refresh)
    /// </summary>
    public async Task<int> BulkAddAsync(
        IEnumerable<Promotion> promotions,
        CancellationToken cancellationToken = default)
    {
        if (promotions == null || !promotions.Any())
            return 0;

        try
        {
            var now = DateTime.UtcNow;
            foreach (var promotion in promotions)
            {
                promotion.LastUpdated = now;
            }

            await _context.Promotions.AddRangeAsync(promotions, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Bulk added {Count} promotions", promotions.Count());
            return promotions.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk adding promotions");
            throw;
        }
    }
}
