using AgenticShopper.Core.Models;

namespace AgenticShopper.Agents.PriceComparison.Interfaces;

/// <summary>
/// Interface for store catalog services (Coles, Woolworths)
/// </summary>
public interface IStoreCatalogService
{
    /// <summary>
    /// Store name (Coles, Woolworths)
    /// </summary>
    string StoreName { get; }

    /// <summary>
    /// Fetch current promotions from store catalog
    /// </summary>
    Task<IEnumerable<Promotion>> FetchPromotionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for a specific product in current promotions
    /// </summary>
    Task<Promotion?> SearchPromotionAsync(
        string productName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get catalog week identifier (e.g., "2025-W50")
    /// </summary>
    string GetCurrentCatalogWeek();
}
