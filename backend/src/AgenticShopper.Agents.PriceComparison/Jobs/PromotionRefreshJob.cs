using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.PriceComparison.Jobs;

/// <summary>
/// Background job for weekly promotion data refresh
/// Runs every Sunday at 1:00 AM to fetch latest promotions from catalogs
/// </summary>
public class PromotionRefreshJob : BackgroundService
{
    private readonly PriceAgent _priceAgent;
    private readonly ILogger<PromotionRefreshJob> _logger;
    private readonly TimeSpan _refreshInterval;
    private readonly TimeSpan _dailyCheckInterval = TimeSpan.FromHours(1);

    public PromotionRefreshJob(
        PriceAgent priceAgent,
        ILogger<PromotionRefreshJob> logger)
    {
        _priceAgent = priceAgent;
        _logger = logger;
        
        // Default: Weekly refresh (7 days)
        _refreshInterval = TimeSpan.FromDays(7);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PromotionRefreshJob started. Refresh interval: {Interval}", _refreshInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Check if it's Sunday at 1:00 AM (UTC)
                if (ShouldRefreshNow(now))
                {
                    _logger.LogInformation("Starting weekly promotion refresh at {Time}", now);
                    await RefreshPromotionsAsync(stoppingToken);
                }
                else
                {
                    _logger.LogDebug("Next refresh scheduled for Sunday 1:00 AM UTC. Current time: {Time}", now);
                }

                // Check every hour to see if it's time to refresh
                await Task.Delay(_dailyCheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PromotionRefreshJob is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PromotionRefreshJob");
                
                // Wait before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        _logger.LogInformation("PromotionRefreshJob stopped");
    }

    /// <summary>
    /// Determine if promotions should be refreshed now
    /// Runs on Sundays at 1:00 AM UTC (when catalogs typically update)
    /// </summary>
    private bool ShouldRefreshNow(DateTime currentTime)
    {
        // Check if it's Sunday
        if (currentTime.DayOfWeek != DayOfWeek.Sunday)
        {
            return false;
        }

        // Check if it's between 1:00 AM and 2:00 AM UTC
        var hour = currentTime.Hour;
        return hour == 1;
    }

    /// <summary>
    /// Execute the promotion refresh operation
    /// </summary>
    private async Task RefreshPromotionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Executing promotion refresh job");

            var result = await _priceAgent.RefreshPromotionsAsync(cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Promotion refresh completed successfully. Added {Count} promotions",
                    result.Data);
            }
            else
            {
                _logger.LogError(
                    "Promotion refresh failed: {Error}",
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing promotion refresh");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PromotionRefreshJob is stopping gracefully");
        await base.StopAsync(cancellationToken);
    }
}
