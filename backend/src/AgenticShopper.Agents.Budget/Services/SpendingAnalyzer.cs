using AgenticShopper.Core.Models;
using AgenticShopper.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.Budget.Services;

/// <summary>
/// Spending trend data for a time period
/// </summary>
public class SpendingTrend
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalSpent { get; set; }
    public Dictionary<string, decimal> CategoryBreakdown { get; set; } = new();
    public Dictionary<string, decimal> StoreBreakdown { get; set; } = new();
    public int TransactionCount { get; set; }
}

/// <summary>
/// Time series data point for charting
/// </summary>
public class DataPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Service for analyzing spending patterns and trends
/// </summary>
public class SpendingAnalyzer
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SpendingAnalyzer> _logger;

    public SpendingAnalyzer(ApplicationDbContext context, ILogger<SpendingAnalyzer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get spending trend for a specific period
    /// </summary>
    public async Task<SpendingTrend> GetSpendingTrendAsync(
        Guid familyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var receipts = await _context.Receipts
                .Include(r => r.Purchases)
                    .ThenInclude(p => p.Product)
                        .ThenInclude(prod => prod!.Category)
                .Where(r => r.FamilyId == familyId
                    && r.PurchaseDate >= startDate
                    && r.PurchaseDate <= endDate)
                .ToListAsync(cancellationToken);

            var trend = new SpendingTrend
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalSpent = receipts.Sum(r => r.TotalAmount),
                TransactionCount = receipts.Count
            };

            // Category breakdown
            trend.CategoryBreakdown = receipts
                .SelectMany(r => r.Purchases ?? Enumerable.Empty<Purchase>())
                .Where(p => p.Product?.Category != null)
                .GroupBy(p => p.Product!.Category!.Name)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(p => p.TotalPrice)
                );

            // Store breakdown
            trend.StoreBreakdown = receipts
                .GroupBy(r => r.StoreName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r => r.TotalAmount)
                );

            _logger.LogInformation(
                "Analyzed spending for family {FamilyId} from {StartDate} to {EndDate}: ${TotalSpent}",
                familyId, startDate, endDate, trend.TotalSpent);

            return trend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing spending trend for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Get weekly spending trends for the last N weeks
    /// </summary>
    public async Task<List<DataPoint>> GetWeeklyTrendsAsync(
        Guid familyId,
        int numberOfWeeks = 12,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-7 * numberOfWeeks);

            var receipts = await _context.Receipts
                .Where(r => r.FamilyId == familyId
                    && r.PurchaseDate >= startDate
                    && r.PurchaseDate <= endDate)
                .ToListAsync(cancellationToken);

            var dataPoints = new List<DataPoint>();

            for (int i = 0; i < numberOfWeeks; i++)
            {
                var weekStart = startDate.AddDays(7 * i);
                var weekEnd = weekStart.AddDays(7);

                var weeklyTotal = receipts
                    .Where(r => r.PurchaseDate >= weekStart && r.PurchaseDate < weekEnd)
                    .Sum(r => r.TotalAmount);

                dataPoints.Add(new DataPoint
                {
                    Date = weekStart,
                    Value = weeklyTotal,
                    Label = $"Week of {weekStart:MMM dd}"
                });
            }

            _logger.LogInformation("Calculated {Count} weekly data points for family {FamilyId}",
                dataPoints.Count, familyId);

            return dataPoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating weekly trends for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Get monthly spending trends for the last N months
    /// </summary>
    public async Task<List<DataPoint>> GetMonthlyTrendsAsync(
        Guid familyId,
        int numberOfMonths = 12,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddMonths(-numberOfMonths);

            var receipts = await _context.Receipts
                .Where(r => r.FamilyId == familyId
                    && r.PurchaseDate >= startDate
                    && r.PurchaseDate <= endDate)
                .ToListAsync(cancellationToken);

            var dataPoints = new List<DataPoint>();

            for (int i = 0; i < numberOfMonths; i++)
            {
                var monthStart = startDate.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1);

                var monthlyTotal = receipts
                    .Where(r => r.PurchaseDate >= monthStart && r.PurchaseDate < monthEnd)
                    .Sum(r => r.TotalAmount);

                dataPoints.Add(new DataPoint
                {
                    Date = monthStart,
                    Value = monthlyTotal,
                    Label = monthStart.ToString("MMM yyyy")
                });
            }

            _logger.LogInformation("Calculated {Count} monthly data points for family {FamilyId}",
                dataPoints.Count, familyId);

            return dataPoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating monthly trends for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Get quarterly spending trends
    /// </summary>
    public async Task<List<DataPoint>> GetQuarterlyTrendsAsync(
        Guid familyId,
        int numberOfQuarters = 4,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddMonths(-3 * numberOfQuarters);

            var receipts = await _context.Receipts
                .Where(r => r.FamilyId == familyId
                    && r.PurchaseDate >= startDate
                    && r.PurchaseDate <= endDate)
                .ToListAsync(cancellationToken);

            var dataPoints = new List<DataPoint>();

            for (int i = 0; i < numberOfQuarters; i++)
            {
                var quarterStart = startDate.AddMonths(3 * i);
                var quarterEnd = quarterStart.AddMonths(3);

                var quarterlyTotal = receipts
                    .Where(r => r.PurchaseDate >= quarterStart && r.PurchaseDate < quarterEnd)
                    .Sum(r => r.TotalAmount);

                var quarterNum = ((quarterStart.Month - 1) / 3) + 1;
                dataPoints.Add(new DataPoint
                {
                    Date = quarterStart,
                    Value = quarterlyTotal,
                    Label = $"Q{quarterNum} {quarterStart:yyyy}"
                });
            }

            _logger.LogInformation("Calculated {Count} quarterly data points for family {FamilyId}",
                dataPoints.Count, familyId);

            return dataPoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating quarterly trends for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Get category spending comparison
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetCategorySpendingAsync(
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
                    && p.PurchaseDate <= endDate
                    && p.Product!.Category != null)
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
            _logger.LogError(ex, "Error calculating category spending for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Get store spending comparison
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetStoreSpendingAsync(
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
            _logger.LogError(ex, "Error calculating store spending for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Calculate average spending per period
    /// </summary>
    public async Task<decimal> GetAverageSpendingAsync(
        Guid familyId,
        BudgetPeriod period,
        int numberOfPeriods = 6,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endDate = DateTime.UtcNow.Date;
            DateTime startDate;

            if (period == BudgetPeriod.Weekly)
            {
                startDate = endDate.AddDays(-7 * numberOfPeriods);
            }
            else // Monthly
            {
                startDate = endDate.AddMonths(-numberOfPeriods);
            }

            var total = await _context.Receipts
                .Where(r => r.FamilyId == familyId
                    && r.PurchaseDate >= startDate
                    && r.PurchaseDate <= endDate)
                .SumAsync(r => r.TotalAmount, cancellationToken);

            var average = total / numberOfPeriods;

            _logger.LogInformation(
                "Calculated average {Period} spending for family {FamilyId}: ${Average}",
                period, familyId, average);

            return average;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating average spending for family {FamilyId}", familyId);
            throw;
        }
    }
}
