using AgenticShopper.Core.Models;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.ListGenerator.Services;

/// <summary>
/// Classifies product urgency based on purchase patterns
/// </summary>
public class UrgencyClassifier
{
    private readonly ILogger _logger;

    public UrgencyClassifier(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Classify urgency based on days since last purchase and expected frequency
    /// </summary>
    public ItemUrgency ClassifyUrgency(
        int daysSinceLastPurchase,
        double averageDaysBetweenPurchases,
        PurchaseFrequency frequency)
    {
        try
        {
            // Use average days if available, otherwise use frequency-based estimate
            var expectedDays = averageDaysBetweenPurchases > 0
                ? averageDaysBetweenPurchases
                : CalculateExpectedDaysFromFrequency(frequency);

            // Calculate ratio of actual days to expected days
            var ratio = daysSinceLastPurchase / expectedDays;

            // Classification logic:
            // - Overdue: >= 110% of expected time (ratio >= 1.1)
            // - DueThisWeek: 80-110% of expected time (ratio 0.8 to 1.1)
            // - Upcoming: 60-80% of expected time (ratio 0.6 to 0.8)

            if (ratio >= 1.1)
            {
                _logger.LogDebug(
                    "Classified as Overdue: {Days} days (expected {Expected}, ratio {Ratio:F2})",
                    daysSinceLastPurchase, expectedDays, ratio);
                return ItemUrgency.Overdue;
            }

            if (ratio >= 0.8)
            {
                _logger.LogDebug(
                    "Classified as DueThisWeek: {Days} days (expected {Expected}, ratio {Ratio:F2})",
                    daysSinceLastPurchase, expectedDays, ratio);
                return ItemUrgency.DueThisWeek;
            }

            _logger.LogDebug(
                "Classified as Upcoming: {Days} days (expected {Expected}, ratio {Ratio:F2})",
                daysSinceLastPurchase, expectedDays, ratio);
            return ItemUrgency.Upcoming;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error classifying urgency, defaulting to Upcoming");
            return ItemUrgency.Upcoming;
        }
    }

    /// <summary>
    /// Classify urgency with custom thresholds
    /// </summary>
    public ItemUrgency ClassifyUrgencyWithThresholds(
        int daysSinceLastPurchase,
        double averageDaysBetweenPurchases,
        PurchaseFrequency frequency,
        double overdueThreshold = 1.1,
        double dueThreshold = 0.8)
    {
        var expectedDays = averageDaysBetweenPurchases > 0
            ? averageDaysBetweenPurchases
            : CalculateExpectedDaysFromFrequency(frequency);

        var ratio = daysSinceLastPurchase / expectedDays;

        if (ratio >= overdueThreshold)
            return ItemUrgency.Overdue;

        if (ratio >= dueThreshold)
            return ItemUrgency.DueThisWeek;

        return ItemUrgency.Upcoming;
    }

    /// <summary>
    /// Calculate expected days between purchases based on frequency enum
    /// </summary>
    private double CalculateExpectedDaysFromFrequency(PurchaseFrequency frequency)
    {
        return frequency switch
        {
            PurchaseFrequency.Weekly => 7,
            PurchaseFrequency.Fortnightly => 14,
            PurchaseFrequency.Monthly => 30,
            PurchaseFrequency.Quarterly => 90,
            PurchaseFrequency.Annually => 365,
            PurchaseFrequency.Occasional => 180, // ~6 months for occasional
            _ => 30 // Default to monthly
        };
    }

    /// <summary>
    /// Get human-readable urgency description
    /// </summary>
    public string GetUrgencyDescription(ItemUrgency urgency)
    {
        return urgency switch
        {
            ItemUrgency.Overdue => "Overdue - Should have been purchased already",
            ItemUrgency.DueThisWeek => "Due this week - Purchase soon",
            ItemUrgency.Upcoming => "Upcoming - Not urgent yet",
            _ => "Unknown urgency"
        };
    }

    /// <summary>
    /// Get urgency color for UI rendering
    /// </summary>
    public string GetUrgencyColor(ItemUrgency urgency)
    {
        return urgency switch
        {
            ItemUrgency.Overdue => "#dc3545",      // Red
            ItemUrgency.DueThisWeek => "#ffc107",  // Yellow/Amber
            ItemUrgency.Upcoming => "#28a745",     // Green
            _ => "#6c757d"                         // Gray
        };
    }

    /// <summary>
    /// Get urgency icon for UI rendering
    /// </summary>
    public string GetUrgencyIcon(ItemUrgency urgency)
    {
        return urgency switch
        {
            ItemUrgency.Overdue => "⚠️",
            ItemUrgency.DueThisWeek => "⏰",
            ItemUrgency.Upcoming => "📅",
            _ => "❓"
        };
    }

    /// <summary>
    /// Calculate days until item becomes overdue
    /// </summary>
    public int CalculateDaysUntilOverdue(
        int daysSinceLastPurchase,
        double averageDaysBetweenPurchases,
        PurchaseFrequency frequency)
    {
        var expectedDays = averageDaysBetweenPurchases > 0
            ? averageDaysBetweenPurchases
            : CalculateExpectedDaysFromFrequency(frequency);

        var overdueThreshold = expectedDays * 1.1;
        var daysUntilOverdue = (int)(overdueThreshold - daysSinceLastPurchase);

        return Math.Max(0, daysUntilOverdue);
    }

    /// <summary>
    /// Batch classify urgency for multiple products
    /// </summary>
    public Dictionary<Guid, ItemUrgency> ClassifyBatch(
        Dictionary<Guid, (int daysSince, double avgDays, PurchaseFrequency freq)> products)
    {
        var results = new Dictionary<Guid, ItemUrgency>();

        foreach (var (productId, data) in products)
        {
            var urgency = ClassifyUrgency(data.daysSince, data.avgDays, data.freq);
            results[productId] = urgency;
        }

        return results;
    }
}
