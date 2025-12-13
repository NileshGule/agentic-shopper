using AgenticShopper.Core.Interfaces;
using AgenticShopper.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AgenticShopper.Data.Repositories;

public class BudgetRepository : IRepository<Budget>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BudgetRepository> _logger;

    public BudgetRepository(ApplicationDbContext context, ILogger<BudgetRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Budgets
                .Include(b => b.Family)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budget with ID {BudgetId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Budget>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Budgets
                .Include(b => b.Family)
                .Include(b => b.Category)
                .OrderBy(b => b.StartDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all budgets");
            throw;
        }
    }

    public async Task<Budget> AddAsync(Budget entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            // Validate budget
            if (entity.Amount <= 0)
                throw new ArgumentException("Budget amount must be positive", nameof(entity));

            if (entity.AlertThreshold < 0.0m || entity.AlertThreshold > 1.0m)
                throw new ArgumentException("Alert threshold must be between 0.0 and 1.0", nameof(entity));

            if (entity.EndDate <= entity.StartDate)
                throw new ArgumentException("End date must be after start date", nameof(entity));

            await _context.Budgets.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added new budget with ID {BudgetId} for family {FamilyId}", 
                entity.Id, entity.FamilyId);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding budget for family {FamilyId}", entity.FamilyId);
            throw;
        }
    }

    public async Task<Budget> UpdateAsync(Budget entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            _context.Budgets.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated budget with ID {BudgetId}", entity.Id);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating budget with ID {BudgetId}", entity.Id);
            throw;
        }
    }

    public async Task<IEnumerable<Budget>> FindAsync(
        Expression<Func<Budget, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Budgets
                .Include(b => b.Family)
                .Include(b => b.Category)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding budgets");
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var budget = await _context.Budgets.FindAsync([id], cancellationToken);
            if (budget == null)
            {
                _logger.LogWarning("Budget with ID {BudgetId} not found for deletion", id);
                throw new InvalidOperationException($"Budget with ID {id} not found");
            }

            _context.Budgets.Remove(budget);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted budget with ID {BudgetId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting budget with ID {BudgetId}", id);
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
            return await _context.Budgets.AnyAsync(b => b.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of budget with ID {BudgetId}", id);
            throw;
        }
    }

    // Budget-specific methods

    /// <summary>
    /// Get all active budgets for a family
    /// </summary>
    public async Task<IEnumerable<Budget>> GetActiveBudgetsByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.Budgets
                .Include(b => b.Category)
                .Where(b => b.FamilyId == familyId 
                    && b.StartDate <= now 
                    && b.EndDate >= now)
                .OrderBy(b => b.Category!.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active budgets for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Get budget for specific category and period
    /// </summary>
    public async Task<Budget?> GetBudgetByCategoryAndPeriodAsync(
        Guid familyId,
        Guid categoryId,
        BudgetPeriod period,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Budgets
                .Include(b => b.Category)
                .FirstOrDefaultAsync(
                    b => b.FamilyId == familyId 
                        && b.CategoryId == categoryId
                        && b.Period == period
                        && b.StartDate <= date
                        && b.EndDate >= date,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error retrieving budget for family {FamilyId}, category {CategoryId}, period {Period}", 
                familyId, categoryId, period);
            throw;
        }
    }

    /// <summary>
    /// Get budgets approaching or exceeding alert threshold
    /// </summary>
    public async Task<IEnumerable<Budget>> GetBudgetsNearingThresholdAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var budgets = await _context.Budgets
                .Include(b => b.Category)
                .Where(b => b.FamilyId == familyId
                    && b.StartDate <= now
                    && b.EndDate >= now)
                .ToListAsync(cancellationToken); // Use ToListAsync instead of chaining after AsEnumerable
            
            return budgets
                .Where(b => b.CurrentSpent >= (b.Amount * b.AlertThreshold))
                .OrderByDescending(b => b.CurrentSpent / b.Amount)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budgets nearing threshold for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Update current spent amount for a budget
    /// </summary>
    public async Task UpdateCurrentSpentAsync(
        Guid budgetId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var budget = await GetByIdAsync(budgetId, cancellationToken);
            if (budget == null)
                throw new InvalidOperationException($"Budget with ID {budgetId} not found");

            budget.CurrentSpent += amount;
            await UpdateAsync(budget, cancellationToken);

            _logger.LogInformation("Updated budget {BudgetId} current spent to {CurrentSpent}", 
                budgetId, budget.CurrentSpent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating current spent for budget {BudgetId}", budgetId);
            throw;
        }
    }

    /// <summary>
    /// Recalculate current spent from purchases within the budget period
    /// </summary>
    public async Task RecalculateCurrentSpentAsync(
        Guid budgetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var budget = await GetByIdAsync(budgetId, cancellationToken);
            if (budget == null)
                throw new InvalidOperationException($"Budget with ID {budgetId} not found");

            // Get all purchases in the budget's category and date range
            var totalSpent = await _context.Purchases
                .Include(p => p.Product)
                .Where(p => p.Product!.CategoryId == budget.CategoryId
                    && p.PurchaseDate >= budget.StartDate
                    && p.PurchaseDate <= budget.EndDate)
                .SumAsync(p => p.TotalPrice, cancellationToken);

            budget.CurrentSpent = totalSpent;
            await UpdateAsync(budget, cancellationToken);

            _logger.LogInformation("Recalculated budget {BudgetId} current spent: {CurrentSpent}", 
                budgetId, totalSpent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating current spent for budget {BudgetId}", budgetId);
            throw;
        }
    }

    /// <summary>
    /// Get budgets that have expired and need renewal
    /// </summary>
    public async Task<IEnumerable<Budget>> GetExpiredBudgetsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.Budgets
                .Include(b => b.Family)
                .Include(b => b.Category)
                .Where(b => b.EndDate < now)
                .OrderBy(b => b.EndDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expired budgets");
            throw;
        }
    }

    /// <summary>
    /// Create a renewal budget for the next period with same settings
    /// </summary>
    public async Task<Budget> RenewBudgetAsync(
        Guid budgetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existingBudget = await GetByIdAsync(budgetId, cancellationToken);
            if (existingBudget == null)
                throw new InvalidOperationException($"Budget with ID {budgetId} not found");

            // Calculate new period dates
            DateTime newStartDate;
            DateTime newEndDate;

            if (existingBudget.Period == BudgetPeriod.Weekly)
            {
                newStartDate = existingBudget.EndDate.AddDays(1);
                newEndDate = newStartDate.AddDays(7);
            }
            else // Monthly
            {
                newStartDate = existingBudget.EndDate.AddDays(1);
                newEndDate = newStartDate.AddMonths(1);
            }

            var newBudget = new Budget
            {
                Id = Guid.NewGuid(),
                FamilyId = existingBudget.FamilyId,
                CategoryId = existingBudget.CategoryId,
                Amount = existingBudget.Amount,
                Period = existingBudget.Period,
                CurrentSpent = 0,
                StartDate = newStartDate,
                EndDate = newEndDate,
                AlertThreshold = existingBudget.AlertThreshold
            };

            await AddAsync(newBudget, cancellationToken);

            _logger.LogInformation("Renewed budget {OldBudgetId} as new budget {NewBudgetId}", 
                budgetId, newBudget.Id);

            return newBudget;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing budget {BudgetId}", budgetId);
            throw;
        }
    }

    /// <summary>
    /// Get spending history by category for analytics
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetSpendingByCategoryAsync(
        Guid familyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Purchases
                .Include(p => p.Receipt)
                .Include(p => p.Product)
                    .ThenInclude(prod => prod!.Category)
                .Where(p => p.Receipt!.FamilyId == familyId
                    && p.PurchaseDate >= startDate
                    && p.PurchaseDate <= endDate)
                .GroupBy(p => p.Product!.Category!.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(p => p.TotalPrice)
                })
                .ToDictionaryAsync(x => x.Category, x => x.Total, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving spending by category for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Get spending history by store for analytics
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetSpendingByStoreAsync(
        Guid familyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Receipts
                .Where(r => r.FamilyId == familyId
                    && r.PurchaseDate >= startDate
                    && r.PurchaseDate <= endDate)
                .GroupBy(r => r.StoreName)
                .Select(g => new
                {
                    Store = g.Key,
                    Total = g.Sum(r => r.TotalAmount)
                })
                .ToDictionaryAsync(x => x.Store, x => x.Total, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving spending by store for family {FamilyId}", familyId);
            throw;
        }
    }
}
