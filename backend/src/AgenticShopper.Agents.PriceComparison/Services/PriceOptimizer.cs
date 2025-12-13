using AgenticShopper.Core.Models;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.PriceComparison.Services;

/// <summary>
/// Service for calculating optimal shopping strategies and savings
/// </summary>
public class PriceOptimizer
{
    private readonly ILogger<PriceOptimizer> _logger;

    public PriceOptimizer(ILogger<PriceOptimizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate optimal shopping strategy (single store vs split)
    /// </summary>
    public OptimalStrategy CalculateOptimalStrategy(List<ProductPriceInfo> products)
    {
        _logger.LogInformation("Calculating optimal strategy for {Count} products", products.Count);

        var strategy = new OptimalStrategy();

        // Calculate totals for each store
        decimal colesTotal = 0;
        decimal woolworthsTotal = 0;
        int colesAvailableCount = 0;
        int woolworthsAvailableCount = 0;

        foreach (var product in products)
        {
            var colesEffectivePrice = product.ColesSalePrice ?? product.ColesPrice;
            var woolworthsEffectivePrice = product.WoolworthsSalePrice ?? product.WoolworthsPrice;

            if (colesEffectivePrice.HasValue)
            {
                colesTotal += colesEffectivePrice.Value * product.Quantity;
                colesAvailableCount++;
            }

            if (woolworthsEffectivePrice.HasValue)
            {
                woolworthsTotal += woolworthsEffectivePrice.Value * product.Quantity;
                woolworthsAvailableCount++;
            }
        }

        // Single-store strategy
        strategy.ColesOnlyTotal = colesTotal;
        strategy.WoolworthsOnlyTotal = woolworthsTotal;
        strategy.ColesOnlyRecommended = colesTotal < woolworthsTotal;

        // Split strategy - buy each item at cheapest store
        var splitColesPurchases = new List<ProductPurchase>();
        var splitWoolworthsPurchases = new List<ProductPurchase>();
        decimal splitTotal = 0;

        foreach (var product in products)
        {
            var colesEffectivePrice = product.ColesSalePrice ?? product.ColesPrice;
            var woolworthsEffectivePrice = product.WoolworthsSalePrice ?? product.WoolworthsPrice;

            if (!colesEffectivePrice.HasValue && !woolworthsEffectivePrice.HasValue)
            {
                _logger.LogWarning("Product {ProductName} has no price data", product.ProductName);
                continue;
            }

            // Determine best store for this product
            if (colesEffectivePrice.HasValue && !woolworthsEffectivePrice.HasValue)
            {
                // Only available at Coles
                splitColesPurchases.Add(new ProductPurchase
                {
                    ProductName = product.ProductName,
                    Quantity = product.Quantity,
                    Price = colesEffectivePrice.Value,
                    Total = colesEffectivePrice.Value * product.Quantity,
                    IsPromotion = product.ColesSalePrice.HasValue
                });
                splitTotal += colesEffectivePrice.Value * product.Quantity;
            }
            else if (woolworthsEffectivePrice.HasValue && !colesEffectivePrice.HasValue)
            {
                // Only available at Woolworths
                splitWoolworthsPurchases.Add(new ProductPurchase
                {
                    ProductName = product.ProductName,
                    Quantity = product.Quantity,
                    Price = woolworthsEffectivePrice.Value,
                    Total = woolworthsEffectivePrice.Value * product.Quantity,
                    IsPromotion = product.WoolworthsSalePrice.HasValue
                });
                splitTotal += woolworthsEffectivePrice.Value * product.Quantity;
            }
            else
            {
                // Available at both - choose cheaper
                if (colesEffectivePrice.Value <= woolworthsEffectivePrice.Value)
                {
                    splitColesPurchases.Add(new ProductPurchase
                    {
                        ProductName = product.ProductName,
                        Quantity = product.Quantity,
                        Price = colesEffectivePrice.Value,
                        Total = colesEffectivePrice.Value * product.Quantity,
                        IsPromotion = product.ColesSalePrice.HasValue,
                        Savings = (woolworthsEffectivePrice.Value - colesEffectivePrice.Value) * product.Quantity
                    });
                    splitTotal += colesEffectivePrice.Value * product.Quantity;
                }
                else
                {
                    splitWoolworthsPurchases.Add(new ProductPurchase
                    {
                        ProductName = product.ProductName,
                        Quantity = product.Quantity,
                        Price = woolworthsEffectivePrice.Value,
                        Total = woolworthsEffectivePrice.Value * product.Quantity,
                        IsPromotion = product.WoolworthsSalePrice.HasValue,
                        Savings = (colesEffectivePrice.Value - woolworthsEffectivePrice.Value) * product.Quantity
                    });
                    splitTotal += woolworthsEffectivePrice.Value * product.Quantity;
                }
            }
        }

        strategy.SplitTotal = splitTotal;
        strategy.ColesPurchases = splitColesPurchases;
        strategy.WoolworthsPurchases = splitWoolworthsPurchases;

        // Calculate savings
        var singleStoreTotal = Math.Min(colesTotal, woolworthsTotal);
        strategy.SplitSavings = singleStoreTotal - splitTotal;
        strategy.SplitRecommended = strategy.SplitSavings > 0;

        // Overall recommendation
        if (strategy.SplitRecommended && strategy.SplitSavings > 5.00m) // $5 threshold for split strategy
        {
            strategy.RecommendedStrategy = "Split";
            strategy.RecommendedTotal = splitTotal;
            strategy.TotalSavings = strategy.SplitSavings;
        }
        else if (strategy.ColesOnlyRecommended)
        {
            strategy.RecommendedStrategy = "Coles";
            strategy.RecommendedTotal = colesTotal;
            strategy.TotalSavings = woolworthsTotal - colesTotal;
        }
        else
        {
            strategy.RecommendedStrategy = "Woolworths";
            strategy.RecommendedTotal = woolworthsTotal;
            strategy.TotalSavings = colesTotal - woolworthsTotal;
        }

        _logger.LogInformation(
            "Optimal strategy: {Strategy}, Total: ${Total:F2}, Savings: ${Savings:F2}",
            strategy.RecommendedStrategy, strategy.RecommendedTotal, strategy.TotalSavings);

        return strategy;
    }

    /// <summary>
    /// Calculate potential savings for a single product
    /// </summary>
    public decimal CalculateProductSavings(ProductPriceInfo product)
    {
        var colesPrice = product.ColesSalePrice ?? product.ColesPrice;
        var woolworthsPrice = product.WoolworthsSalePrice ?? product.WoolworthsPrice;

        if (!colesPrice.HasValue || !woolworthsPrice.HasValue)
        {
            return 0;
        }

        return Math.Abs(colesPrice.Value - woolworthsPrice.Value) * product.Quantity;
    }

    /// <summary>
    /// Calculate total promotion savings for all products
    /// </summary>
    public decimal CalculatePromotionSavings(List<ProductPriceInfo> products)
    {
        decimal totalSavings = 0;

        foreach (var product in products)
        {
            if (product.ColesPrice.HasValue && product.ColesSalePrice.HasValue)
            {
                totalSavings += (product.ColesPrice.Value - product.ColesSalePrice.Value) * product.Quantity;
            }

            if (product.WoolworthsPrice.HasValue && product.WoolworthsSalePrice.HasValue)
            {
                totalSavings += (product.WoolworthsPrice.Value - product.WoolworthsSalePrice.Value) * product.Quantity;
            }
        }

        return totalSavings;
    }
}

/// <summary>
/// Product price information for optimization
/// </summary>
public class ProductPriceInfo
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? ColesPrice { get; set; }
    public decimal? ColesSalePrice { get; set; }
    public decimal? WoolworthsPrice { get; set; }
    public decimal? WoolworthsSalePrice { get; set; }
}

/// <summary>
/// Optimal shopping strategy result
/// </summary>
public class OptimalStrategy
{
    public decimal ColesOnlyTotal { get; set; }
    public decimal WoolworthsOnlyTotal { get; set; }
    public bool ColesOnlyRecommended { get; set; }

    public decimal SplitTotal { get; set; }
    public List<ProductPurchase> ColesPurchases { get; set; } = new();
    public List<ProductPurchase> WoolworthsPurchases { get; set; } = new();
    public decimal SplitSavings { get; set; }
    public bool SplitRecommended { get; set; }

    public string RecommendedStrategy { get; set; } = "Unknown";
    public decimal RecommendedTotal { get; set; }
    public decimal TotalSavings { get; set; }
}

/// <summary>
/// Individual product purchase in split strategy
/// </summary>
public class ProductPurchase
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
    public bool IsPromotion { get; set; }
    public decimal Savings { get; set; }
}
