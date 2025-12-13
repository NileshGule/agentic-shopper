using AgenticShopper.Agents.PriceComparison.Interfaces;
using AgenticShopper.Core.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace AgenticShopper.Agents.PriceComparison.Services;

/// <summary>
/// Service for fetching Coles catalog promotions
/// </summary>
public class ColesCatalogService : IStoreCatalogService
{
    private readonly ILogger<ColesCatalogService> _logger;

    public string StoreName => "Coles";

    public ColesCatalogService(ILogger<ColesCatalogService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Fetch current promotions from Coles catalog
    /// TODO: Implement actual web scraping or API integration
    /// </summary>
    public async Task<IEnumerable<Promotion>> FetchPromotionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching promotions from Coles catalog");

        // TODO: Replace with actual web scraping implementation
        // For now, return mock data for development
        var catalogWeek = GetCurrentCatalogWeek();
        var now = DateTime.UtcNow;

        var promotions = new List<Promotion>
        {
            new Promotion
            {
                Id = Guid.NewGuid(),
                ProductName = "Milk 2L",
                NormalizedProductName = "MILK 2L",
                StoreName = StoreName,
                OriginalPrice = 4.50m,
                SalePrice = 3.50m,
                DiscountPercentage = 22.22m,
                StartDate = now.AddDays(-3),
                EndDate = now.AddDays(4),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            },
            new Promotion
            {
                Id = Guid.NewGuid(),
                ProductName = "Bread White",
                NormalizedProductName = "BREAD WHITE",
                StoreName = StoreName,
                OriginalPrice = 3.20m,
                SalePrice = 2.50m,
                DiscountPercentage = 21.88m,
                StartDate = now.AddDays(-3),
                EndDate = now.AddDays(4),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            },
            new Promotion
            {
                Id = Guid.NewGuid(),
                ProductName = "Bananas 1kg",
                NormalizedProductName = "BANANAS 1KG",
                StoreName = StoreName,
                OriginalPrice = 4.00m,
                SalePrice = 2.99m,
                DiscountPercentage = 25.25m,
                StartDate = now.AddDays(-3),
                EndDate = now.AddDays(4),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            },
            new Promotion
            {
                Id = Guid.NewGuid(),
                ProductName = "Chicken Breast 1kg",
                NormalizedProductName = "CHICKEN BREAST 1KG",
                StoreName = StoreName,
                OriginalPrice = 13.00m,
                SalePrice = 10.00m,
                DiscountPercentage = 23.08m,
                StartDate = now.AddDays(-3),
                EndDate = now.AddDays(4),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            },
            new Promotion
            {
                Id = Guid.NewGuid(),
                ProductName = "Tomatoes 1kg",
                NormalizedProductName = "TOMATOES 1KG",
                StoreName = StoreName,
                OriginalPrice = 5.50m,
                SalePrice = 4.00m,
                DiscountPercentage = 27.27m,
                StartDate = now.AddDays(-3),
                EndDate = now.AddDays(4),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            },
            new Promotion
            {
                Id = Guid.NewGuid(),
                ProductName = "Pasta 500g",
                NormalizedProductName = "PASTA 500G",
                StoreName = StoreName,
                OriginalPrice = 3.00m,
                SalePrice = 2.00m,
                DiscountPercentage = 33.33m,
                StartDate = now.AddDays(-3),
                EndDate = now.AddDays(4),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            }
        };

        _logger.LogInformation("Fetched {Count} mock promotions from Coles", promotions.Count);

        return await Task.FromResult(promotions);
    }

    /// <summary>
    /// Search for a specific product promotion
    /// </summary>
    public async Task<Promotion?> SearchPromotionAsync(string productName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching Coles promotions for product: {ProductName}", productName);

        var promotions = await FetchPromotionsAsync(cancellationToken);
        var normalizedSearch = productName.ToUpperInvariant().Trim();

        // Try exact match first
        var exactMatch = promotions.FirstOrDefault(p =>
            p.NormalizedProductName.Equals(normalizedSearch, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            _logger.LogInformation("Found exact match for {ProductName}", productName);
            return exactMatch;
        }

        // Try partial match
        var partialMatch = promotions.FirstOrDefault(p =>
            p.NormalizedProductName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
            normalizedSearch.Contains(p.NormalizedProductName, StringComparison.OrdinalIgnoreCase));

        if (partialMatch != null)
        {
            _logger.LogInformation("Found partial match for {ProductName}: {MatchedProduct}",
                productName, partialMatch.ProductName);
        }
        else
        {
            _logger.LogInformation("No promotion found for {ProductName} at Coles", productName);
        }

        return partialMatch;
    }

    /// <summary>
    /// Get current catalog week identifier (e.g., "2025-W50")
    /// </summary>
    public string GetCurrentCatalogWeek()
    {
        var now = DateTime.UtcNow;
        var calendar = CultureInfo.InvariantCulture.Calendar;
        var weekRule = CalendarWeekRule.FirstDay;
        var firstDayOfWeek = DayOfWeek.Monday;

        var weekNumber = calendar.GetWeekOfYear(now, weekRule, firstDayOfWeek);
        var year = now.Year;

        // Handle year boundary (week 1 might be in previous year)
        if (weekNumber == 1 && now.Month == 12)
        {
            year++;
        }

        var catalogWeek = $"{year}-W{weekNumber:D2}";
        _logger.LogDebug("Current catalog week: {CatalogWeek}", catalogWeek);

        return catalogWeek;
    }
}
