using AgenticShopper.Core.Interfaces;
using AgenticShopper.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AgenticShopper.Data.Repositories;

public class ReceiptRepository : IRepository<Receipt>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReceiptRepository> _logger;

    public ReceiptRepository(ApplicationDbContext context, ILogger<ReceiptRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Receipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Receipts
                .Include(r => r.Purchases)
                    .ThenInclude(p => p.Product)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving receipt with ID {ReceiptId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Receipt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Receipts
                .Include(r => r.Purchases)
                    .ThenInclude(p => p.Product)
                .OrderByDescending(r => r.PurchaseDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all receipts");
            throw;
        }
    }

    public async Task<Receipt> AddAsync(Receipt entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            entity.CreatedDate = DateTime.UtcNow;

            await _context.Receipts.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added new receipt with ID {ReceiptId}", entity.Id);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding receipt");
            throw;
        }
    }

    public async Task<Receipt> UpdateAsync(Receipt entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            _context.Receipts.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated receipt with ID {ReceiptId}", entity.Id);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating receipt with ID {ReceiptId}", entity.Id);
            throw;
        }
    }

    public async Task<IEnumerable<Receipt>> FindAsync(
        Expression<Func<Receipt, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Receipts
                .Include(r => r.Purchases)
                    .ThenInclude(p => p.Product)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding receipts");
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var receipt = await _context.Receipts.FindAsync([id], cancellationToken);
            if (receipt == null)
            {
                _logger.LogWarning("Receipt with ID {ReceiptId} not found for deletion", id);
                throw new InvalidOperationException($"Receipt with ID {id} not found");
            }

            _context.Receipts.Remove(receipt);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted receipt with ID {ReceiptId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting receipt with ID {ReceiptId}", id);
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
            return await _context.Receipts.AnyAsync(r => r.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of receipt with ID {ReceiptId}", id);
            throw;
        }
    }

    // Receipt-specific methods
    public async Task<IEnumerable<Receipt>> GetByFamilyIdAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Receipts
                .Include(r => r.Purchases)
                    .ThenInclude(p => p.Product)
                .Where(r => r.FamilyId == familyId)
                .OrderByDescending(r => r.PurchaseDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving receipts for family {FamilyId}", familyId);
            throw;
        }
    }

    public async Task<IEnumerable<Receipt>> GetByDateRangeAsync(
        Guid familyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Receipts
                .Include(r => r.Purchases)
                    .ThenInclude(p => p.Product)
                .Where(r => r.FamilyId == familyId 
                    && r.PurchaseDate >= startDate 
                    && r.PurchaseDate <= endDate)
                .OrderByDescending(r => r.PurchaseDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving receipts for family {FamilyId} between {StartDate} and {EndDate}",
                familyId,
                startDate,
                endDate);
            throw;
        }
    }

    public async Task<IEnumerable<Receipt>> GetPendingReceiptsAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Receipts
                .Include(r => r.Purchases)
                    .ThenInclude(p => p.Product)
                .Where(r => r.FamilyId == familyId && r.Status == ReceiptStatus.Pending)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending receipts for family {FamilyId}", familyId);
            throw;
        }
    }
}
