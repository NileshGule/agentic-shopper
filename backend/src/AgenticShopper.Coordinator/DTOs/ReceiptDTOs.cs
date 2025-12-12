namespace AgenticShopper.Coordinator.DTOs;

/// <summary>
/// Request DTO for uploading a receipt
/// </summary>
public class UploadReceiptRequest
{
    public required Guid FamilyId { get; init; }
    public required Guid UploadedBy { get; init; }
}

/// <summary>
/// Response DTO for receipt upload
/// </summary>
public class UploadReceiptResponse
{
    public Guid ReceiptId { get; init; }
    public string? StoreName { get; init; }
    public DateTime PurchaseDate { get; init; }
    public decimal TotalAmount { get; init; }
    public int ItemCount { get; init; }
    public double ConfidenceScore { get; init; }
    public string? Status { get; init; }
    public bool NeedsReview { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// DTO for receipt summary in list views
/// </summary>
public class ReceiptSummaryDto
{
    public Guid Id { get; init; }
    public string? StoreName { get; init; }
    public DateTime PurchaseDate { get; init; }
    public decimal TotalAmount { get; init; }
    public int ItemCount { get; init; }
    public double ConfidenceScore { get; init; }
    public string? Status { get; init; }
    public DateTime CreatedDate { get; init; }
    public string? ImageUrl { get; init; }
}

/// <summary>
/// DTO for detailed receipt view
/// </summary>
public class ReceiptDetailDto
{
    public Guid Id { get; init; }
    public Guid FamilyId { get; init; }
    public Guid UploadedBy { get; init; }
    public string? StoreName { get; init; }
    public DateTime PurchaseDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string? ImageUrl { get; init; }
    public double ConfidenceScore { get; init; }
    public string? Status { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? VerifiedDate { get; init; }
    public List<PurchaseDto> Purchases { get; init; } = new();
}

/// <summary>
/// DTO for purchase line item
/// </summary>
public class PurchaseDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public string? CategoryName { get; init; }
}

/// <summary>
/// Request DTO for updating receipt details
/// </summary>
public class UpdateReceiptRequest
{
    public string? StoreName { get; init; }
    public DateTime? PurchaseDate { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? Status { get; init; }
    public List<UpdatePurchaseRequest>? Purchases { get; init; }
}

/// <summary>
/// Request DTO for updating purchase line item
/// </summary>
public class UpdatePurchaseRequest
{
    public Guid? Id { get; init; }
    public string? ProductName { get; init; }
    public decimal? Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? TotalPrice { get; init; }
}
