using AgenticShopper.Agents.Budget.Services;
using AgenticShopper.Core.Abstractions;
using AgenticShopper.Core.Models;
using AgenticShopper.Data.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.Budget;

/// <summary>
/// Request to create a new budget
/// </summary>
public class CreateBudgetRequest
{
    public Guid FamilyId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public BudgetPeriod Period { get; set; }
    public decimal? AlertThreshold { get; set; } // Optional, defaults to 0.90
}

/// <summary>
/// Request to update a budget
/// </summary>
public class UpdateBudgetRequest
{
    public Guid BudgetId { get; set; }
    public decimal? Amount { get; set; }
    public decimal? AlertThreshold { get; set; }
}

/// <summary>
/// Request for spending analytics
/// </summary>
public class SpendingAnalyticsRequest
{
    public Guid FamilyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string TrendType { get; set; } = "monthly"; // weekly, monthly, quarterly
}

/// <summary>
/// Request for budget status
/// </summary>
public class BudgetStatusRequest
{
    public Guid FamilyId { get; set; }
}

/// <summary>
/// Budget status response
/// </summary>
public class BudgetStatusResponse
{
    public List<BudgetSummary> Budgets { get; set; } = new();
    public List<BudgetAlert> Alerts { get; set; } = new();
    public decimal TotalBudgeted { get; set; }
    public decimal TotalSpent { get; set; }
}

public class BudgetSummary
{
    public Guid BudgetId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal CurrentSpent { get; set; }
    public decimal Remaining { get; set; }
    public decimal PercentageUsed { get; set; }
    public BudgetPeriod Period { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "On Track"; // On Track, Warning, Over Budget
}

/// <summary>
/// Spending analytics response
/// </summary>
public class SpendingAnalyticsResponse
{
    public List<DataPoint> TrendData { get; set; } = new();
    public Dictionary<string, decimal> CategoryBreakdown { get; set; } = new();
    public Dictionary<string, decimal> StoreBreakdown { get; set; } = new();
    public decimal TotalSpent { get; set; }
    public int TransactionCount { get; set; }
    public decimal AveragePerTransaction { get; set; }
}

/// <summary>
/// Agent responsible for budget tracking and spending analysis
/// </summary>
public class BudgetAgent : AgentBase
{
    private readonly BudgetRepository _budgetRepository;
    private readonly SpendingAnalyzer _spendingAnalyzer;
    private readonly BudgetAlertService _alertService;

    public override string AgentId => "budget-agent";
    public override string AgentName => "Budget Tracking Agent";
    public override string Description => "Tracks budgets, analyzes spending patterns, and generates alerts";

    public BudgetAgent(
        BudgetRepository budgetRepository,
        SpendingAnalyzer spendingAnalyzer,
        BudgetAlertService alertService,
        ILogger<BudgetAgent> logger,
        IChatClient? chatClient = null)
        : base(logger, chatClient)
    {
        _budgetRepository = budgetRepository;
        _spendingAnalyzer = spendingAnalyzer;
        _alertService = alertService;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Initializing BudgetAgent");
        await base.InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// Execute budget tracking and analytics operations
    /// </summary>
    protected override async Task<AgentResult<TOutput>> OnExecuteAsync<TInput, TOutput>(
        TInput input,
        CancellationToken cancellationToken = default)
    {
        if (input is CreateBudgetRequest createRequest && typeof(TOutput) == typeof(Core.Models.Budget))
        {
            var result = await CreateBudgetAsync(createRequest, cancellationToken);
            return AgentResult<TOutput>.Success((TOutput)(object)result);
        }

        if (input is UpdateBudgetRequest updateRequest && typeof(TOutput) == typeof(Core.Models.Budget))
        {
            var result = await UpdateBudgetAsync(updateRequest, cancellationToken);
            return AgentResult<TOutput>.Success((TOutput)(object)result);
        }

        if (input is BudgetStatusRequest statusRequest && typeof(TOutput) == typeof(BudgetStatusResponse))
        {
            var result = await GetBudgetStatusAsync(statusRequest, cancellationToken);
            return AgentResult<TOutput>.Success((TOutput)(object)result);
        }

        if (input is SpendingAnalyticsRequest analyticsRequest && typeof(TOutput) == typeof(SpendingAnalyticsResponse))
        {
            var result = await GetSpendingAnalyticsAsync(analyticsRequest, cancellationToken);
            return AgentResult<TOutput>.Success((TOutput)(object)result);
        }

        return AgentResult<TOutput>.Failure($"Unsupported request type: {typeof(TInput).Name}");
    }

    /// <summary>
    /// Create a new budget
    /// </summary>
    private async Task<Core.Models.Budget> CreateBudgetAsync(
        CreateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation(
                "Creating budget for family {FamilyId}, category {CategoryId}, amount ${Amount}",
                request.FamilyId, request.CategoryId, request.Amount);

            // Calculate period dates
            var startDate = DateTime.UtcNow.Date;
            DateTime endDate;

            if (request.Period == BudgetPeriod.Weekly)
            {
                endDate = startDate.AddDays(7);
            }
            else // Monthly
            {
                endDate = startDate.AddMonths(1);
            }

            var budget = new Core.Models.Budget
            {
                Id = Guid.NewGuid(),
                FamilyId = request.FamilyId,
                CategoryId = request.CategoryId,
                Amount = request.Amount,
                Period = request.Period,
                CurrentSpent = 0,
                StartDate = startDate,
                EndDate = endDate,
                AlertThreshold = request.AlertThreshold ?? 0.90m
            };

            await _budgetRepository.AddAsync(budget, cancellationToken);

            Logger.LogInformation("Created budget {BudgetId}", budget.Id);

            return budget;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating budget for family {FamilyId}", request.FamilyId);
            throw;
        }
    }

    /// <summary>
    /// Update an existing budget
    /// </summary>
    private async Task<Core.Models.Budget> UpdateBudgetAsync(
        UpdateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var budget = await _budgetRepository.GetByIdAsync(request.BudgetId, cancellationToken);
            if (budget == null)
            {
                throw new InvalidOperationException($"Budget with ID {request.BudgetId} not found");
            }

            if (request.Amount.HasValue)
            {
                budget.Amount = request.Amount.Value;
            }

            if (request.AlertThreshold.HasValue)
            {
                budget.AlertThreshold = request.AlertThreshold.Value;
            }

            await _budgetRepository.UpdateAsync(budget, cancellationToken);

            Logger.LogInformation("Updated budget {BudgetId}", request.BudgetId);

            return budget;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating budget {BudgetId}", request.BudgetId);
            throw;
        }
    }

    /// <summary>
    /// Get current budget status and alerts
    /// </summary>
    private async Task<BudgetStatusResponse> GetBudgetStatusAsync(
        BudgetStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var budgets = await _budgetRepository.GetActiveBudgetsByFamilyAsync(
                request.FamilyId, cancellationToken);

            var response = new BudgetStatusResponse();

            foreach (var budget in budgets)
            {
                var percentageUsed = budget.Amount > 0
                    ? (budget.CurrentSpent / budget.Amount) * 100
                    : 0;

                var status = "On Track";
                if (budget.CurrentSpent >= budget.Amount)
                {
                    status = "Over Budget";
                }
                else if (percentageUsed >= budget.AlertThreshold * 100)
                {
                    status = "Warning";
                }

                response.Budgets.Add(new BudgetSummary
                {
                    BudgetId = budget.Id,
                    CategoryName = budget.Category?.Name ?? "Unknown",
                    Amount = budget.Amount,
                    CurrentSpent = budget.CurrentSpent,
                    Remaining = budget.Amount - budget.CurrentSpent,
                    PercentageUsed = Math.Round(percentageUsed, 1),
                    Period = budget.Period,
                    StartDate = budget.StartDate,
                    EndDate = budget.EndDate,
                    Status = status
                });
            }

            // Get active alerts
            response.Alerts = await _alertService.GetActiveAlertsAsync(
                request.FamilyId, cancellationToken);

            // Calculate totals
            response.TotalBudgeted = response.Budgets.Sum(b => b.Amount);
            response.TotalSpent = response.Budgets.Sum(b => b.CurrentSpent);

            Logger.LogInformation(
                "Retrieved budget status for family {FamilyId}: {BudgetCount} budgets, {AlertCount} alerts",
                request.FamilyId, response.Budgets.Count, response.Alerts.Count);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting budget status for family {FamilyId}", request.FamilyId);
            throw;
        }
    }

    /// <summary>
    /// Get spending analytics and trends
    /// </summary>
    private async Task<SpendingAnalyticsResponse> GetSpendingAnalyticsAsync(
        SpendingAnalyticsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = new SpendingAnalyticsResponse();

            // Get trend data based on type
            response.TrendData = request.TrendType.ToLower() switch
            {
                "weekly" => await _spendingAnalyzer.GetWeeklyTrendsAsync(
                    request.FamilyId, 12, cancellationToken),
                "quarterly" => await _spendingAnalyzer.GetQuarterlyTrendsAsync(
                    request.FamilyId, 4, cancellationToken),
                _ => await _spendingAnalyzer.GetMonthlyTrendsAsync(
                    request.FamilyId, 12, cancellationToken)
            };

            // Get category breakdown
            response.CategoryBreakdown = await _spendingAnalyzer.GetCategorySpendingAsync(
                request.FamilyId, request.StartDate, request.EndDate, cancellationToken);

            // Get store breakdown
            response.StoreBreakdown = await _spendingAnalyzer.GetStoreSpendingAsync(
                request.FamilyId, request.StartDate, request.EndDate, cancellationToken);

            // Get overall spending trend
            var trend = await _spendingAnalyzer.GetSpendingTrendAsync(
                request.FamilyId, request.StartDate, request.EndDate, cancellationToken);

            response.TotalSpent = trend.TotalSpent;
            response.TransactionCount = trend.TransactionCount;
            response.AveragePerTransaction = trend.TransactionCount > 0
                ? trend.TotalSpent / trend.TransactionCount
                : 0;

            Logger.LogInformation(
                "Retrieved spending analytics for family {FamilyId}: ${TotalSpent} over {TransactionCount} transactions",
                request.FamilyId, response.TotalSpent, response.TransactionCount);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting spending analytics for family {FamilyId}", request.FamilyId);
            throw;
        }
    }

    /// <summary>
    /// Recalculate spending for a purchase
    /// </summary>
    public async Task UpdateBudgetSpendingAsync(
        Guid familyId,
        Guid categoryId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var budget = await _budgetRepository.GetBudgetByCategoryAndPeriodAsync(
                familyId, categoryId, BudgetPeriod.Monthly, now, cancellationToken);

            if (budget != null)
            {
                await _budgetRepository.UpdateCurrentSpentAsync(budget.Id, amount, cancellationToken);

                // Check if alert should be triggered
                var alert = _alertService.CheckBudgetThreshold(budget);
                if (alert != null && _alertService.ShouldSendAlert(budget))
                {
                    Logger.LogWarning(
                        "Budget alert triggered for budget {BudgetId}: {Message}",
                        budget.Id, alert.Message);

                    // In a real implementation, this would publish to a message queue
                    // or send a notification through Azure Service Bus

                    await _alertService.MarkAlertSentAsync(budget.Id, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, 
                "Error updating budget spending for family {FamilyId}, category {CategoryId}",
                familyId, categoryId);
            throw;
        }
    }
}
