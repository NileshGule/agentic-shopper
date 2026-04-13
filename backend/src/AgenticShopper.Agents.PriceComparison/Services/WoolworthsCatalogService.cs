using AgenticShopper.Agents.PriceComparison.Interfaces;
using AgenticShopper.Core.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace AgenticShopper.Agents.PriceComparison.Services;

/// <summary>
/// Service for fetching Woolworths catalog promotions via web scraping
/// </summary>
public class WoolworthsCatalogService : IStoreCatalogService
{
    private readonly ILogger<WoolworthsCatalogService> _logger;
    private readonly HttpClient _httpClient;
    private const string WoolworthsSpecialsUrl = "https://www.woolworths.com.au/shop/specials";

    public string StoreName => "Woolworths";

    public WoolworthsCatalogService(ILogger<WoolworthsCatalogService> logger, IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger;
        _httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    /// <summary>
    /// Fetch current promotions from Woolworths catalog using web scraping
    /// </summary>
    public async Task<IEnumerable<Promotion>> FetchPromotionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching promotions from Woolworths catalog");

        try
        {
            // Attempt to scrape live data from Woolworths website
            var livePromotions = await ScrapeWoolworthsWebsiteAsync(cancellationToken);
            if (livePromotions.Any())
            {
                _logger.LogInformation("Successfully scraped {Count} promotions from Woolworths website", livePromotions.Count());
                return livePromotions;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scrape Woolworths website, falling back to mock data");
        }

        // Fallback to mock data if scraping fails
        _logger.LogInformation("Using mock data for Woolworths promotions");
        return GenerateMockPromotions();
    }

    /// <summary>
    /// Scrape Woolworths website for current promotions
    /// </summary>
    private async Task<IEnumerable<Promotion>> ScrapeWoolworthsWebsiteAsync(CancellationToken cancellationToken)
    {
        var promotions = new List<Promotion>();

        try
        {
            var response = await _httpClient.GetAsync(WoolworthsSpecialsUrl, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Woolworths specials page: {StatusCode}", response.StatusCode);
                return promotions;
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Parse Woolworths product cards (structure may vary, this is an example)
            var productNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'product-tile')] | //article[contains(@class, 'product')]");

            if (productNodes == null || !productNodes.Any())
            {
                _logger.LogWarning("No product nodes found on Woolworths website");
                return promotions;
            }

            var catalogWeek = GetCurrentCatalogWeek();
            var now = DateTime.UtcNow;

            foreach (var node in productNodes)
            {
                try
                {
                    var promotion = ParseWoolworthsProductNode(node, catalogWeek, now);
                    if (promotion != null)
                    {
                        promotions.Add(promotion);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse individual Woolworths product node");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping Woolworths website");
        }

        return promotions;
    }

    /// <summary>
    /// Parse a single Woolworths product node into a Promotion
    /// </summary>
    private Promotion? ParseWoolworthsProductNode(HtmlNode node, string catalogWeek, DateTime now)
    {
        // Extract product name
        var nameNode = node.SelectSingleNode(".//h3 | .//span[contains(@class, 'product-title')]");
        if (nameNode == null) return null;

        var productName = CleanText(nameNode.InnerText);
        if (string.IsNullOrWhiteSpace(productName)) return null;

        // Extract prices
        var salePriceNode = node.SelectSingleNode(".//span[contains(@class, 'primary')] | .//div[contains(@class, 'price-dollars')]");
        var originalPriceNode = node.SelectSingleNode(".//span[contains(@class, 'was')] | .//span[contains(@class, 'price-was')]");

        if (salePriceNode == null) return null;

        var salePrice = ParsePrice(salePriceNode.InnerText);
        var originalPrice = originalPriceNode != null ? ParsePrice(originalPriceNode.InnerText) : salePrice * 1.2m;

        if (salePrice <= 0) return null;

        var discountPercentage = originalPrice > salePrice 
            ? Math.Round((originalPrice - salePrice) / originalPrice * 100, 2) 
            : 0;

        return new Promotion
        {
            Id = Guid.NewGuid(),
            ProductName = productName,
            NormalizedProductName = productName.ToUpperInvariant().Trim(),
            StoreName = StoreName,
            OriginalPrice = originalPrice,
            SalePrice = salePrice,
            DiscountPercentage = discountPercentage,
            StartDate = now.AddDays(-2),
            EndDate = now.AddDays(5),
            CatalogWeek = catalogWeek,
            LastUpdated = now
        };
    }

    /// <summary>
    /// Parse price from text (e.g., "$4.50" -> 4.50)
    /// </summary>
    private decimal ParsePrice(string priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText)) return 0;

        // Remove currency symbols and whitespace
        var cleaned = Regex.Replace(priceText, @"[^\d.]", "");
        
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
        {
            return price;
        }

        return 0;
    }

    /// <summary>
    /// Clean HTML text content
    /// </summary>
    private string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Decode HTML entities and clean whitespace
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    /// <summary>
    /// Generate mock promotions for development/fallback
    /// </summary>
    private IEnumerable<Promotion> GenerateMockPromotions()
    {
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
                OriginalPrice = 4.60m,
                SalePrice = 3.80m,
                DiscountPercentage = 17.39m,
                StartDate = now.AddDays(-2),
                EndDate = now.AddDays(5),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            },
            new Promotion
            {
                Id = Guid.NewGuid(),
                ProductName = "Bread White",
                NormalizedProductName = "BREAD WHITE",
                StoreName = StoreName,
                OriginalPrice = 3.00m,
                SalePrice = 2.30m,
                DiscountPercentage = 23.33m,
                StartDate = now.AddDays(-2),
                EndDate = now.AddDays(5),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            },
            new Promotion
            {
                Id = Guid.NewGuid(),
                ProductName = "Bananas 1kg",
                NormalizedProductName = "BANANAS 1KG",
                StoreName = StoreName,
                OriginalPrice = 3.90m,
                SalePrice = 3.20m,
                DiscountPercentage = 17.95m,
                StartDate = now.AddDays(-2),
                EndDate = now.AddDays(5),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            },
            new Promotion
            {
                Id = Guid.NewGuid(),
                ProductName = "Chicken Breast 1kg",
                NormalizedProductName = "CHICKEN BREAST 1KG",
                StoreName = StoreName,
                OriginalPrice = 12.50m,
                SalePrice = 9.50m,
                DiscountPercentage = 24.00m,
                StartDate = now.AddDays(-2),
                EndDate = now.AddDays(5),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            },
            new Promotion
            {
                Id = Guid.NewGuid(),
                ProductName = "Tomatoes 1kg",
                NormalizedProductName = "TOMATOES 1KG",
                StoreName = StoreName,
                OriginalPrice = 5.80m,
                SalePrice = 4.50m,
                DiscountPercentage = 22.41m,
                StartDate = now.AddDays(-2),
                EndDate = now.AddDays(5),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            },
            new Promotion
            {
                Id = Guid.NewGuid(),
                ProductName = "Rice 1kg",
                NormalizedProductName = "RICE 1KG",
                StoreName = StoreName,
                OriginalPrice = 5.00m,
                SalePrice = 3.50m,
                DiscountPercentage = 30.00m,
                StartDate = now.AddDays(-2),
                EndDate = now.AddDays(5),
                CatalogWeek = catalogWeek,
                LastUpdated = now
            }
        };

        _logger.LogInformation("Fetched {Count} mock promotions from Woolworths", promotions.Count);

        return promotions;
    }

    /// <summary>
    /// Search for a specific product promotion
    /// </summary>
    public async Task<Promotion?> SearchPromotionAsync(string productName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching Woolworths promotions for product: {ProductName}", productName);

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
            _logger.LogInformation("No promotion found for {ProductName} at Woolworths", productName);
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
