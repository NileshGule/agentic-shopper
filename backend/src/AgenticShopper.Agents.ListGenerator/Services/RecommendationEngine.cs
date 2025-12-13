using AgenticShopper.Core.Models;
using AgenticShopper.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.ListGenerator.Services;

/// <summary>
/// Product recommendation from frequency analysis
/// </summary>
public class ProductRecommendation
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public PurchaseFrequency Frequency { get; set; }
    public double AverageDaysBetweenPurchases { get; set; }
    public DateTime? LastPurchased { get; set; }
    public int DaysSinceLastPurchase { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Engine for generating product recommendations based on purchase frequency
/// </summary>
public class RecommendationEngine
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;

    public RecommendationEngine(ApplicationDbContext dbContext, ILogger logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get product recommendations for a family based on frequency analysis
    /// </summary>
    public async Task<List<ProductRecommendation>> GetRecommendationsAsync(
        Guid familyId,
        bool includeDueItems,
        bool includeOverdueItems,
        bool includeUpcomingItems,
        List<Guid> categoryFilter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting recommendations for family {FamilyId} (Due: {Due}, Overdue: {Overdue}, Upcoming: {Upcoming})",
                familyId, includeDueItems, includeOverdueItems, includeUpcomingItems);

            // Get all products with frequency data for this family
            var query = _dbContext.ProductFrequencies
                .Include(pf => pf.Product)
                    .ThenInclude(p => p!.Category)
                .Where(pf => pf.FamilyId == familyId && !pf.IsPaused);

            // Apply category filter if specified
            if (categoryFilter.Any())
            {
                query = query.Where(pf => pf.Product != null && 
                                         categoryFilter.Contains(pf.Product.CategoryId!.Value));
            }

            var frequencies = await query.ToListAsync(cancellationToken);

            var recommendations = new List<ProductRecommendation>();
            var now = DateTime.UtcNow;

            foreach (var frequency in frequencies)
            {
                if (frequency.Product == null)
                    continue;

                // Calculate days since last purchase
                var daysSinceLastPurchase = frequency.LastPurchaseDate.HasValue
                    ? (int)(now - frequency.LastPurchaseDate.Value).TotalDays
                    : int.MaxValue;

                // Calculate expected days between purchases based on frequency
                var expectedDays = CalculateExpectedDays(frequency.Frequency);

                // Determine if product should be recommended
                var shouldRecommend = ShouldRecommend(
                    daysSinceLastPurchase,
                    expectedDays,
                    includeDueItems,
                    includeOverdueItems,
                    includeUpcomingItems);

                if (shouldRecommend)
                {
                    recommendations.Add(new ProductRecommendation
                    {
                        ProductId = frequency.ProductId,
                        ProductName = frequency.Product.Name,
                        CategoryName = frequency.Product.Category?.Name,
                        Frequency = frequency.Frequency,
                        AverageDaysBetweenPurchases = frequency.AverageDaysBetweenPurchases,
                        LastPurchased = frequency.LastPurchaseDate,
                        DaysSinceLastPurchase = daysSinceLastPurchase,
                        Notes = frequency.Product.Notes
                    });
                }
            }

            _logger.LogInformation(
                "Generated {Count} recommendations for family {FamilyId}",
                recommendations.Count, familyId);

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Calculate expected days between purchases for a given frequency
    /// </summary>
    private double CalculateExpectedDays(PurchaseFrequency frequency)
    {
        return frequency switch
        {
            PurchaseFrequency.Weekly => 7,
            PurchaseFrequency.Fortnightly => 14,
            PurchaseFrequency.Monthly => 30,
            PurchaseFrequency.Quarterly => 90,
            PurchaseFrequency.Annually => 365,
            PurchaseFrequency.Occasional => 180, // Default to ~6 months for occasional items
            _ => 30
        };
    }

    /// <summary>
    /// Determine if a product should be recommended based on purchase timing
    /// </summary>
    private bool ShouldRecommend(
        int daysSinceLastPurchase,
        double expectedDays,
        bool includeDueItems,
        bool includeOverdueItems,
        bool includeUpcomingItems)
    {
        // Calculate ratio of actual days to expected days
        var ratio = daysSinceLastPurchase / expectedDays;

        // Overdue: ratio >= 1.1 (10% past expected)
        if (ratio >= 1.1 && includeOverdueItems)
            return true;

        // Due this week: ratio between 0.8 and 1.1 (within 20% of expected)
        if (ratio >= 0.8 && ratio < 1.1 && includeDueItems)
            return true;

        // Upcoming: ratio between 0.6 and 0.8 (60-80% of expected time)
        if (ratio >= 0.6 && ratio < 0.8 && includeUpcomingItems)
            return true;

        return false;
    }

    /// <summary>
    /// Get recommendations grouped by category
    /// </summary>
    public async Task<Dictionary<string, List<ProductRecommendation>>> GetRecommendationsByCategoryAsync(
        Guid familyId,
        bool includeDueItems,
        bool includeOverdueItems,
        bool includeUpcomingItems,
        CancellationToken cancellationToken = default)
    {
        var recommendations = await GetRecommendationsAsync(
            familyId,
            includeDueItems,
            includeOverdueItems,
            includeUpcomingItems,
            new List<Guid>(),
            cancellationToken);

        return recommendations
            .GroupBy(r => r.CategoryName ?? "Uncategorized")
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Get count of recommendations by urgency
    /// </summary>
    public async Task<(int overdue, int dueThisWeek, int upcoming)> GetRecommendationCountsAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var frequencies = await _dbContext.ProductFrequencies
            .Where(pf => pf.FamilyId == familyId && !pf.IsPaused)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        int overdueCount = 0;
        int dueThisWeekCount = 0;
        int upcomingCount = 0;

        foreach (var frequency in frequencies)
        {
            var daysSinceLastPurchase = frequency.LastPurchaseDate.HasValue
                ? (int)(now - frequency.LastPurchaseDate.Value).TotalDays
                : int.MaxValue;

            var expectedDays = CalculateExpectedDays(frequency.Frequency);
            var ratio = daysSinceLastPurchase / expectedDays;

            if (ratio >= 1.1)
                overdueCount++;
            else if (ratio >= 0.8)
                dueThisWeekCount++;
            else if (ratio >= 0.6)
                upcomingCount++;
        }

        return (overdueCount, dueThisWeekCount, upcomingCount);
    }
}
