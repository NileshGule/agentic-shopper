using AgenticShopper.Core.Models;
using AgenticShopper.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.Budget.Services;

/// <summary>
/// Budget alert information
/// </summary>
public class BudgetAlert
{
    public Guid BudgetId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal CurrentSpent { get; set; }
    public decimal AlertThreshold { get; set; }
    public decimal PercentageUsed { get; set; }
    public decimal Remaining { get; set; }
    public bool IsOverBudget { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Service for monitoring budgets and generating alerts
/// </summary>
public class BudgetAlertService
{
    private readonly BudgetRepository _budgetRepository;
    private readonly ILogger<BudgetAlertService> _logger;

    public BudgetAlertService(
        BudgetRepository budgetRepository,
        ILogger<BudgetAlertService> logger)
    {
        _budgetRepository = budgetRepository ?? throw new ArgumentNullException(nameof(budgetRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Check all budgets for a family and generate alerts
    /// </summary>
    public async Task<List<BudgetAlert>> CheckBudgetsAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var budgets = await _budgetRepository.GetActiveBudgetsByFamilyAsync(
                familyId, cancellationToken);

            var alerts = new List<BudgetAlert>();

            foreach (var budget in budgets)
            {
                var alert = CheckBudgetThreshold(budget);
                if (alert != null)
                {
                    alerts.Add(alert);
                }
            }

            if (alerts.Any())
            {
                _logger.LogInformation("Generated {Count} budget alerts for family {FamilyId}",
                    alerts.Count, familyId);
            }

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking budgets for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Check a single budget against its threshold
    /// </summary>
    public BudgetAlert? CheckBudgetThreshold(Core.Models.Budget budget)
    {
        try
        {
            var percentageUsed = budget.Amount > 0
                ? (budget.CurrentSpent / budget.Amount) * 100
                : 0;

            var thresholdPercentage = budget.AlertThreshold * 100;

            // Only create alert if threshold is reached
            if (percentageUsed < thresholdPercentage)
            {
                return null;
            }

            var alert = new BudgetAlert
            {
                BudgetId = budget.Id,
                CategoryName = budget.Category?.Name ?? "Unknown",
                Amount = budget.Amount,
                CurrentSpent = budget.CurrentSpent,
                AlertThreshold = budget.AlertThreshold,
                PercentageUsed = Math.Round(percentageUsed, 1),
                Remaining = budget.Amount - budget.CurrentSpent,
                IsOverBudget = budget.CurrentSpent >= budget.Amount,
                PeriodStart = budget.StartDate,
                PeriodEnd = budget.EndDate
            };

            // Generate appropriate message
            if (alert.IsOverBudget)
            {
                var overAmount = budget.CurrentSpent - budget.Amount;
                alert.Message = $"Budget exceeded! You've spent ${budget.CurrentSpent:F2} " +
                    $"(${overAmount:F2} over budget) in {alert.CategoryName}. " +
                    $"Budget period ends {budget.EndDate:MMM dd, yyyy}.";
            }
            else
            {
                alert.Message = $"Warning: You've used {percentageUsed:F1}% of your " +
                    $"{alert.CategoryName} budget (${budget.CurrentSpent:F2} of ${budget.Amount:F2}). " +
                    $"${alert.Remaining:F2} remaining until {budget.EndDate:MMM dd, yyyy}.";
            }

            _logger.LogInformation(
                "Budget alert for budget {BudgetId} ({CategoryName}): {PercentageUsed}% used",
                budget.Id, alert.CategoryName, percentageUsed);

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking threshold for budget {BudgetId}", budget.Id);
            throw;
        }
    }

    /// <summary>
    /// Get budgets that should trigger alerts
    /// </summary>
    public async Task<List<BudgetAlert>> GetActiveAlertsAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var budgetsNearingThreshold = await _budgetRepository
                .GetBudgetsNearingThresholdAsync(familyId, cancellationToken);

            var alerts = new List<BudgetAlert>();

            foreach (var budget in budgetsNearingThreshold)
            {
                var alert = CheckBudgetThreshold(budget);
                if (alert != null)
                {
                    alerts.Add(alert);
                }
            }

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Mark alert as sent for a budget
    /// </summary>
    public async Task MarkAlertSentAsync(
        Guid budgetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var budget = await _budgetRepository.GetByIdAsync(budgetId, cancellationToken);
            if (budget == null)
            {
                throw new InvalidOperationException($"Budget with ID {budgetId} not found");
            }

            budget.LastAlertSent = DateTime.UtcNow;
            await _budgetRepository.UpdateAsync(budget, cancellationToken);

            _logger.LogInformation("Marked alert as sent for budget {BudgetId}", budgetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking alert as sent for budget {BudgetId}", budgetId);
            throw;
        }
    }

    /// <summary>
    /// Check if budget should send alert (prevent spam)
    /// </summary>
    public bool ShouldSendAlert(Core.Models.Budget budget)
    {
        // Don't send if alert was sent in the last 24 hours
        if (budget.LastAlertSent.HasValue)
        {
            var hoursSinceLastAlert = (DateTime.UtcNow - budget.LastAlertSent.Value).TotalHours;
            if (hoursSinceLastAlert < 24)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Auto-renew expired budgets
    /// </summary>
    public async Task AutoRenewBudgetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredBudgets = await _budgetRepository.GetExpiredBudgetsAsync(cancellationToken);

            var renewedCount = 0;

            foreach (var budget in expiredBudgets)
            {
                await _budgetRepository.RenewBudgetAsync(budget.Id, cancellationToken);
                renewedCount++;
            }

            if (renewedCount > 0)
            {
                _logger.LogInformation("Auto-renewed {Count} expired budgets", renewedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-renewing budgets");
            throw;
        }
    }

    /// <summary>
    /// Recalculate all budget spending from actual purchases
    /// </summary>
    public async Task RecalculateAllBudgetsAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var budgets = await _budgetRepository.GetActiveBudgetsByFamilyAsync(
                familyId, cancellationToken);

            foreach (var budget in budgets)
            {
                await _budgetRepository.RecalculateCurrentSpentAsync(budget.Id, cancellationToken);
            }

            _logger.LogInformation(
                "Recalculated spending for {Count} budgets for family {FamilyId}",
                budgets.Count(), familyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating budgets for family {FamilyId}", familyId);
            throw;
        }
    }
}
