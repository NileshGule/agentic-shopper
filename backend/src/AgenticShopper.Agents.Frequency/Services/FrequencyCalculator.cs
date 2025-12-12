using AgenticShopper.Core.Models;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.Frequency.Services;

/// <summary>
/// Result of frequency calculation
/// </summary>
public class FrequencyResult
{
    public PurchaseFrequency Frequency { get; set; }
    public double AverageDaysBetweenPurchases { get; set; }
    public int PurchaseCount { get; set; }
    public DateTime? NextExpectedPurchase { get; set; }
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// Service for calculating purchase frequency patterns
/// Requires minimum 3 purchases for reliable frequency calculation
/// </summary>
public class FrequencyCalculator
{
    private readonly ILogger _logger;
    private const int MinimumPurchasesRequired = 3;

    // Frequency thresholds (days ± tolerance)
    private static readonly Dictionary<PurchaseFrequency, (double days, double tolerance)> FrequencyThresholds = new()
    {
        { PurchaseFrequency.Weekly, (7, 2) },
        { PurchaseFrequency.Fortnightly, (14, 3) },
        { PurchaseFrequency.Monthly, (30, 5) },
        { PurchaseFrequency.Quarterly, (90, 10) },
        { PurchaseFrequency.Annually, (365, 30) }
    };

    public FrequencyCalculator(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate purchase frequency from purchase history
    /// </summary>
    public FrequencyResult CalculateFrequency(List<DateTime> purchaseDates)
    {
        _logger.LogInformation("Calculating frequency for {Count} purchases", purchaseDates.Count);

        // Insufficient data
        if (purchaseDates.Count < MinimumPurchasesRequired)
        {
            return new FrequencyResult
            {
                Frequency = PurchaseFrequency.Unknown,
                PurchaseCount = purchaseDates.Count,
                Confidence = 0.0,
                Reasoning = $"Insufficient data: {purchaseDates.Count} purchase(s), minimum {MinimumPurchasesRequired} required"
            };
        }

        // Sort dates chronologically
        var sortedDates = purchaseDates.OrderBy(d => d).ToList();

        // Calculate intervals between purchases
        var intervals = new List<double>();
        for (int i = 1; i < sortedDates.Count; i++)
        {
            var daysBetween = (sortedDates[i] - sortedDates[i - 1]).TotalDays;
            intervals.Add(daysBetween);
        }

        var averageInterval = intervals.Average();
        var stdDeviation = CalculateStandardDeviation(intervals);
        
        // Determine frequency category
        var (frequency, confidence) = DetermineFrequency(averageInterval, stdDeviation);

        // Calculate next expected purchase
        var lastPurchase = sortedDates.Last();
        var nextExpected = lastPurchase.AddDays(averageInterval);

        var result = new FrequencyResult
        {
            Frequency = frequency,
            AverageDaysBetweenPurchases = averageInterval,
            PurchaseCount = purchaseDates.Count,
            NextExpectedPurchase = nextExpected,
            Confidence = confidence,
            Reasoning = BuildReasoning(frequency, averageInterval, stdDeviation, purchaseDates.Count)
        };

        _logger.LogInformation("Calculated frequency: {Frequency} (avg: {Avg} days, confidence: {Confidence})",
            frequency, averageInterval, confidence);

        return result;
    }

    /// <summary>
    /// Determine frequency category based on average interval
    /// </summary>
    private (PurchaseFrequency frequency, double confidence) DetermineFrequency(
        double averageInterval,
        double stdDeviation)
    {
        // Check each frequency threshold
        foreach (var (frequency, (targetDays, tolerance)) in FrequencyThresholds)
        {
            if (Math.Abs(averageInterval - targetDays) <= tolerance)
            {
                // Calculate confidence based on standard deviation
                // Lower std deviation = higher confidence
                var variationRatio = stdDeviation / averageInterval;
                var confidence = Math.Max(0.5, 1.0 - variationRatio);
                
                return (frequency, Math.Round(confidence, 2));
            }
        }

        // If no match, classify as Occasional
        // High variation or irregular pattern
        var occasionalConfidence = stdDeviation > averageInterval * 0.5 ? 0.6 : 0.7;
        
        return (PurchaseFrequency.Occasional, occasionalConfidence);
    }

    /// <summary>
    /// Calculate standard deviation of intervals
    /// </summary>
    private double CalculateStandardDeviation(List<double> values)
    {
        if (values.Count < 2)
            return 0;

        var average = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow(v - average, 2));
        var variance = sumOfSquares / values.Count;
        
        return Math.Sqrt(variance);
    }

    /// <summary>
    /// Build human-readable reasoning for the frequency determination
    /// </summary>
    private string BuildReasoning(
        PurchaseFrequency frequency,
        double avgDays,
        double stdDev,
        int purchaseCount)
    {
        var consistency = stdDev < avgDays * 0.2 ? "consistent" : 
                         stdDev < avgDays * 0.5 ? "moderately consistent" : 
                         "irregular";

        return frequency switch
        {
            PurchaseFrequency.Weekly => 
                $"Purchased approximately every {avgDays:F1} days ({consistency} pattern) based on {purchaseCount} purchases",
            
            PurchaseFrequency.Fortnightly => 
                $"Purchased approximately every {avgDays:F1} days ({consistency} pattern) based on {purchaseCount} purchases",
            
            PurchaseFrequency.Monthly => 
                $"Purchased approximately every {avgDays:F1} days ({consistency} pattern) based on {purchaseCount} purchases",
            
            PurchaseFrequency.Quarterly => 
                $"Purchased approximately every {avgDays:F1} days ({consistency} pattern) based on {purchaseCount} purchases",
            
            PurchaseFrequency.Annually => 
                $"Purchased approximately every {avgDays:F1} days ({consistency} pattern) based on {purchaseCount} purchases",
            
            PurchaseFrequency.Occasional => 
                $"Irregular pattern with {avgDays:F1} days average between purchases (variation: {stdDev:F1} days)",
            
            _ => $"Insufficient data for frequency determination ({purchaseCount} purchases)"
        };
    }

    /// <summary>
    /// Detect if purchase pattern has changed significantly
    /// </summary>
    public bool HasPatternChanged(List<DateTime> allPurchases, int recentPurchaseWindow = 3)
    {
        if (allPurchases.Count < MinimumPurchasesRequired + recentPurchaseWindow)
            return false;

        var sorted = allPurchases.OrderBy(d => d).ToList();
        
        // Calculate frequency for all purchases
        var overallFrequency = CalculateFrequency(sorted);
        
        // Calculate frequency for recent purchases only
        var recentPurchases = sorted.TakeLast(recentPurchaseWindow).ToList();
        var recentFrequency = CalculateFrequency(recentPurchases);

        // Pattern has changed if frequency category is different
        return overallFrequency.Frequency != recentFrequency.Frequency;
    }

    /// <summary>
    /// Calculate urgency score for shopping list generation
    /// 0.0 = not urgent, 1.0 = very urgent (past due)
    /// </summary>
    public double CalculateUrgency(DateTime? nextExpectedPurchase, DateTime currentDate)
    {
        if (!nextExpectedPurchase.HasValue)
            return 0.0;

        var daysUntilExpected = (nextExpectedPurchase.Value - currentDate).TotalDays;

        if (daysUntilExpected < -7)
            return 1.0; // Very overdue (more than a week past)
        else if (daysUntilExpected < 0)
            return 0.9; // Overdue
        else if (daysUntilExpected <= 1)
            return 0.8; // Due today or tomorrow
        else if (daysUntilExpected <= 3)
            return 0.6; // Due soon (2-3 days)
        else if (daysUntilExpected <= 7)
            return 0.4; // Coming up (within a week)
        else if (daysUntilExpected <= 14)
            return 0.2; // Future (1-2 weeks)
        else
            return 0.1; // Not urgent (more than 2 weeks)
    }

    /// <summary>
    /// Suggest optimal reorder window (days before next expected purchase)
    /// </summary>
    public int SuggestReorderWindow(PurchaseFrequency frequency)
    {
        return frequency switch
        {
            PurchaseFrequency.Weekly => 2,        // 2 days before
            PurchaseFrequency.Fortnightly => 3,   // 3 days before
            PurchaseFrequency.Monthly => 5,       // 5 days before
            PurchaseFrequency.Quarterly => 10,    // 10 days before
            PurchaseFrequency.Annually => 30,     // 30 days before
            PurchaseFrequency.Occasional => 7,    // 1 week before
            _ => 3                                 // Default: 3 days
        };
    }
}
