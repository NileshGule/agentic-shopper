namespace AgenticShopper.Agents.Receipt.Interfaces;

/// <summary>
/// Service interface for Optical Character Recognition (OCR) operations on receipt images
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Extract text and structured data from a receipt image
    /// </summary>
    /// <param name="imageStream">Stream containing the receipt image</param>
    /// <param name="fileName">Original filename for logging/debugging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OCR result with extracted receipt data</returns>
    Task<OcrResult> ExtractReceiptDataAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the OCR service is properly configured and available
    /// </summary>
    /// <returns>True if service is available, false otherwise</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Gets the name of this OCR provider
    /// </summary>
    string ProviderName { get; }
}

/// <summary>
/// Result of OCR processing operation
/// </summary>
public class OcrResult
{
    /// <summary>
    /// Whether the OCR operation was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Store name extracted from receipt
    /// </summary>
    public string? StoreName { get; init; }

    /// <summary>
    /// Purchase date extracted from receipt
    /// </summary>
    public DateTime? PurchaseDate { get; init; }

    /// <summary>
    /// Total amount from receipt
    /// </summary>
    public decimal? TotalAmount { get; init; }

    /// <summary>
    /// Individual line items extracted from receipt
    /// </summary>
    public List<OcrLineItem> LineItems { get; init; } = new();

    /// <summary>
    /// Confidence score of the OCR operation (0.0 - 1.0)
    /// </summary>
    public double ConfidenceScore { get; init; }

    /// <summary>
    /// Raw extracted text from the receipt
    /// </summary>
    public string? RawText { get; init; }

    /// <summary>
    /// Error message if OCR failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Name of the OCR provider that processed this receipt
    /// </summary>
    public string? ProviderName { get; init; }
}

/// <summary>
/// Individual line item extracted from receipt
/// </summary>
public class OcrLineItem
{
    /// <summary>
    /// Product name/description
    /// </summary>
    public required string ProductName { get; init; }

    /// <summary>
    /// Quantity purchased
    /// </summary>
    public decimal Quantity { get; init; } = 1;

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// Total price for this line item (quantity * unit price)
    /// </summary>
    public decimal TotalPrice { get; init; }

    /// <summary>
    /// Confidence score for this line item (0.0 - 1.0)
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Raw text of the line item from OCR
    /// </summary>
    public string? RawText { get; init; }
}
