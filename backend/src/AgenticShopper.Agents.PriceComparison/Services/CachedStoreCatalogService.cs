using AgenticShopper.Agents.PriceComparison.Interfaces;
using AgenticShopper.Core.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgenticShopper.Agents.PriceComparison.Services;

/// <summary>
/// Decorator that adds Redis caching to store catalog services
/// Implements T116 and T118: Redis caching for promotion data with 7-day TTL
/// </summary>
public class CachedStoreCatalogService : IStoreCatalogService
{
    private readonly IStoreCatalogService _innerService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedStoreCatalogService> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(7);

    public string StoreName => _innerService.StoreName;

    public CachedStoreCatalogService(
        IStoreCatalogService innerService,
        IDistributedCache cache,
        ILogger<CachedStoreCatalogService> logger)
    {
        _innerService = innerService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Fetch promotions with Redis caching (7-day TTL)
    /// </summary>
    public async Task<IEnumerable<Promotion>> FetchPromotionsAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"promotions:{StoreName}:all";

        try
        {
            // Try to get from cache first
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("Cache hit for {StoreName} promotions", StoreName);
                var promotions = JsonSerializer.Deserialize<List<Promotion>>(cachedData);
                
                if (promotions != null && promotions.Any())
                {
                    return promotions;
                }
            }

            _logger.LogInformation("Cache miss for {StoreName} promotions, fetching from source", StoreName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read from cache for {StoreName}, fetching from source", StoreName);
        }

        // Fetch from underlying service
        var freshPromotions = await _innerService.FetchPromotionsAsync(cancellationToken);
        var promotionsList = freshPromotions.ToList();

        if (promotionsList.Any())
        {
            try
            {
                // Cache the results with 7-day expiration
                var serialized = JsonSerializer.Serialize(promotionsList);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheDuration
                };

                await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);
                _logger.LogInformation("Cached {Count} promotions for {StoreName} with 7-day TTL",
                    promotionsList.Count, StoreName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache promotions for {StoreName}", StoreName);
            }
        }

        return promotionsList;
    }

    /// <summary>
    /// Search for a specific product promotion with caching
    /// </summary>
    public async Task<Promotion?> SearchPromotionAsync(string productName, CancellationToken cancellationToken = default)
    {
        var normalizedProductName = productName.ToUpperInvariant().Trim();
        var cacheKey = $"promotions:{StoreName}:product:{normalizedProductName}";

        try
        {
            // Try to get from cache first
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("Cache hit for {StoreName} product: {ProductName}", StoreName, productName);
                return JsonSerializer.Deserialize<Promotion>(cachedData);
            }

            _logger.LogDebug("Cache miss for {StoreName} product: {ProductName}", StoreName, productName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read product cache for {StoreName}: {ProductName}", StoreName, productName);
        }

        // Search using underlying service
        var promotion = await _innerService.SearchPromotionAsync(productName, cancellationToken);

        if (promotion != null)
        {
            try
            {
                // Cache the individual product result with 7-day expiration
                var serialized = JsonSerializer.Serialize(promotion);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheDuration
                };

                await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);
                _logger.LogDebug("Cached promotion for {StoreName} product: {ProductName}", StoreName, productName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache product promotion for {StoreName}: {ProductName}",
                    StoreName, productName);
            }
        }

        return promotion;
    }

    /// <summary>
    /// Get current catalog week
    /// </summary>
    public string GetCurrentCatalogWeek()
    {
        return _innerService.GetCurrentCatalogWeek();
    }

    /// <summary>
    /// Invalidate all cached promotions for this store
    /// </summary>
    public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"promotions:{StoreName}:all";
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            _logger.LogInformation("Invalidated cache for {StoreName} promotions", StoreName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache for {StoreName}", StoreName);
        }
    }
}
