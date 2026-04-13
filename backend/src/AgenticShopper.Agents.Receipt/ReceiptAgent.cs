using AgenticShopper.Agents.Receipt.Interfaces;
using AgenticShopper.Agents.Receipt.Services;
using AgenticShopper.Core.Abstractions;
using AgenticShopper.Core.Interfaces;
using AgenticShopper.Core.Models;
using AgenticShopper.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.Receipt;

/// <summary>
/// Agent responsible for processing receipt images, extracting data via OCR, and creating receipt entities
/// </summary>
public class ReceiptAgent : AgentBase
{
    private readonly IOcrService _ocrService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IRepository<Core.Models.Receipt> _receiptRepository;
    private readonly IRepository<Product> _productRepository;

    public override string AgentId => "receipt-agent";
    public override string AgentName => "Receipt Processing Agent";
    public override string Description => "Processes receipt images using OCR to extract purchase data";

    public ReceiptAgent(
        ILogger<ReceiptAgent> logger,
        IOcrService ocrService,
        IBlobStorageService blobStorageService,
        IRepository<Core.Models.Receipt> receiptRepository,
        IRepository<Product> productRepository,
        IChatClient? chatClient = null)
        : base(logger, chatClient)
    {
        _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _receiptRepository = receiptRepository ?? throw new ArgumentNullException(nameof(receiptRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    protected override async Task<AgentResult<TOutput>> OnExecuteAsync<TInput, TOutput>(
        TInput input,
        CancellationToken cancellationToken)
    {
        if (input is not ReceiptProcessingRequest request)
        {
            return AgentResult<TOutput>.Failure("Invalid input type. Expected ReceiptProcessingRequest.");
        }

        try
        {
            Logger.LogInformation(
                "Processing receipt for family {FamilyId}, uploaded by {UserId}",
                request.FamilyId,
                request.UploadedBy);

            // Step 1: Upload image to blob storage
            var imageUrl = await _blobStorageService.UploadAsync(
                fileName: request.FileName,
                content: request.ImageStream,
                containerName: "receipts");

            Logger.LogInformation("Receipt image uploaded to {ImageUrl}", imageUrl);

            // Step 2: Extract data using OCR
            request.ImageStream.Position = 0; // Reset stream for OCR
            var ocrResult = await _ocrService.ExtractReceiptDataAsync(
                imageStream: request.ImageStream,
                fileName: request.FileName,
                cancellationToken: cancellationToken);

            if (!ocrResult.IsSuccess)
            {
                Logger.LogError("OCR processing failed: {ErrorMessage}", ocrResult.ErrorMessage);
                return AgentResult<TOutput>.Failure($"OCR processing failed: {ocrResult.ErrorMessage}");
            }

            Logger.LogInformation(
                "OCR extraction successful with confidence {Confidence:P}, found {ItemCount} items",
                ocrResult.ConfidenceScore,
                ocrResult.LineItems.Count);

            // Step 3: Create receipt entity
            var purchaseDate = ocrResult.PurchaseDate ?? DateTime.UtcNow;
            // Ensure the date is UTC (Npgsql requires DateTimeKind.Utc for timestamptz columns)
            if (purchaseDate.Kind != DateTimeKind.Utc)
                purchaseDate = DateTime.SpecifyKind(purchaseDate, DateTimeKind.Utc);

            var receipt = new Core.Models.Receipt
            {
                Id = Guid.NewGuid(),
                FamilyId = request.FamilyId,
                UploadedBy = request.UploadedBy,
                StoreName = ocrResult.StoreName ?? "Unknown Store",
                PurchaseDate = purchaseDate,
                TotalAmount = ocrResult.TotalAmount ?? 0,
                ImageUrl = imageUrl,
                ConfidenceScore = ocrResult.ConfidenceScore,
                Status = DetermineReceiptStatus(ocrResult.ConfidenceScore),
                CreatedDate = DateTime.UtcNow
            };

            // Step 4: Process line items and create/update products
            var purchases = new List<Purchase>();
            foreach (var lineItem in ocrResult.LineItems)
            {
                // T080 [FR-010]: Find existing product or create a new one
                var product = await GetOrCreateProductAsync(
                    createdBy: request.UploadedBy,
                    productName: lineItem.ProductName,
                    cancellationToken: cancellationToken);

                var purchase = new Purchase
                {
                    Id = Guid.NewGuid(),
                    ReceiptId = receipt.Id,
                    ProductId = product.Id,
                    Product = product,
                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    TotalPrice = lineItem.TotalPrice,
                    PurchaseDate = receipt.PurchaseDate
                };
                purchases.Add(purchase);
            }

            receipt.Purchases = purchases;

            // Step 5: Save receipt and all related entities to the database
            await _receiptRepository.AddAsync(receipt, cancellationToken);

            Logger.LogInformation(
                "Successfully created receipt {ReceiptId} with {PurchaseCount} purchases",
                receipt.Id,
                receipt.Purchases.Count);

            // Step 6: Prepare the result
            var result = new ReceiptProcessingResult
            {
                ReceiptId = receipt.Id,
                StoreName = receipt.StoreName,
                PurchaseDate = receipt.PurchaseDate,
                TotalAmount = receipt.TotalAmount,
                ItemCount = receipt.Purchases.Count,
                ConfidenceScore = receipt.ConfidenceScore,
                Status = receipt.Status.ToString(),
                NeedsReview = receipt.Status == ReceiptStatus.NeedsReview
            };

            return AgentResult<TOutput>.Success((TOutput)(object)result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred during receipt processing.");
            return AgentResult<TOutput>.Failure($"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines the status of the receipt based on the OCR confidence score.
    /// </summary>
    private static ReceiptStatus DetermineReceiptStatus(double confidenceScore)
    {
        return confidenceScore switch
        {
            >= 0.9 => ReceiptStatus.Verified,
            >= 0.7 => ReceiptStatus.NeedsReview,
            _ => ReceiptStatus.Pending
        };
    }

    /// <summary>
    /// Finds a product by name for a given user or creates a new one if it doesn't exist.
    /// </summary>
    private async Task<Product> GetOrCreateProductAsync(Guid createdBy, string productName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            productName = "Unknown Product";
        }

        // Normalize product name for better matching
        var normalizedProductName = productName.Trim().ToLower();

        // T081 [FR-011]: Search for an existing product (case-insensitive)
        var existingProducts = await _productRepository.FindAsync(
            p => p.CreatedBy == createdBy && p.NormalizedName == normalizedProductName,
            cancellationToken);

        var existingProduct = existingProducts.FirstOrDefault();

        if (existingProduct != null)
        {
            Logger.LogDebug("Found existing product '{ProductName}' with ID {ProductId}", existingProduct.Name, existingProduct.Id);
            return existingProduct;
        }

        // T082 [FR-012]: Create a new product if not found
        Logger.LogInformation("Creating new product '{ProductName}' for user {CreatedBy}", productName, createdBy);
        var newProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = productName,
            NormalizedName = normalizedProductName,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow
        };

        // Note: We don't save the product here. It will be saved as part of the receipt graph.
        return newProduct;
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Receipt Agent initialized with OCR provider: {Provider}", _ocrService.ProviderName);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Input request for receipt processing
/// </summary>
public class ReceiptProcessingRequest
{
    public required Stream ImageStream { get; init; }
    public required string FileName { get; init; }
    public required Guid FamilyId { get; init; }
    public required Guid UploadedBy { get; init; }
}

/// <summary>
/// Result of receipt processing
/// </summary>
public class ReceiptProcessingResult
{
    public Guid ReceiptId { get; init; }
    public string? StoreName { get; init; }
    public DateTime PurchaseDate { get; init; }
    public decimal TotalAmount { get; init; }
    public int ItemCount { get; init; }
    public double ConfidenceScore { get; init; }
    public string? Status { get; init; }
    public bool NeedsReview { get; init; }
}
