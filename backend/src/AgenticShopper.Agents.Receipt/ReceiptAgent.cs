using AgenticShopper.Agents.Receipt.Interfaces;
using AgenticShopper.Agents.Receipt.Services;
using AgenticShopper.Core.Abstractions;
using AgenticShopper.Core.Interfaces;
using AgenticShopper.Core.Models;
using AgenticShopper.Core.Services;
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
        ILlmProvider llmProvider,
        IOcrService ocrService,
        IBlobStorageService blobStorageService,
        IRepository<Core.Models.Receipt> receiptRepository,
        IRepository<Product> productRepository)
        : base(logger, llmProvider)
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
            var receipt = new Core.Models.Receipt
            {
                Id = Guid.NewGuid(),
                FamilyId = request.FamilyId,
                UploadedBy = request.UploadedBy,
                StoreName = ocrResult.StoreName ?? "Unknown Store",
                PurchaseDate = ocrResult.PurchaseDate ?? DateTime.UtcNow,
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
                var product = await GetOrCreateProductAsync(
                    lineItem.ProductName,
                    request.FamilyId,
                    request.UploadedBy,
                    cancellationToken);

                var purchase = new Purchase
                {
                    Id = Guid.NewGuid(),
                    ReceiptId = receipt.Id,
                    ProductId = product.Id,
                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    TotalPrice = lineItem.TotalPrice,
                    PurchaseDate = receipt.PurchaseDate
                };

                purchases.Add(purchase);
                receipt.Purchases.Add(purchase);

                Logger.LogDebug(
                    "Created purchase: {ProductName} x {Quantity} @ ${UnitPrice}",
                    product.Name,
                    purchase.Quantity,
                    purchase.UnitPrice);
            }

            // Step 5: Save receipt to database
            await _receiptRepository.AddAsync(receipt, cancellationToken);

            Logger.LogInformation(
                "Receipt {ReceiptId} created successfully with {PurchaseCount} purchases, status: {Status}",
                receipt.Id,
                purchases.Count,
                receipt.Status);

            // Step 6: Use LLM to improve OCR results if confidence is low
            if (receipt.Status == ReceiptStatus.NeedsReview)
            {
                Logger.LogInformation("Receipt has low confidence, may need LLM enhancement");
                // TODO: Implement LLM-based OCR enhancement
                // var enhancedResult = await EnhanceWithLlmAsync(ocrResult, cancellationToken);
            }

            var result = new ReceiptProcessingResult
            {
                ReceiptId = receipt.Id,
                StoreName = receipt.StoreName,
                PurchaseDate = receipt.PurchaseDate,
                TotalAmount = receipt.TotalAmount,
                ItemCount = purchases.Count,
                ConfidenceScore = receipt.ConfidenceScore,
                Status = receipt.Status.ToString(),
                NeedsReview = receipt.Status == ReceiptStatus.NeedsReview
            };

            return AgentResult<TOutput>.Success(
                (TOutput)(object)result,
                new Dictionary<string, object>
                {
                    { "ocrProvider", ocrResult.ProviderName ?? "Unknown" },
                    { "processingTime", DateTime.UtcNow }
                });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process receipt");
            return AgentResult<TOutput>.Failure($"Receipt processing failed: {ex.Message}", ex);
        }
    }

    private async Task<Product> GetOrCreateProductAsync(
        string productName,
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeProductName(productName);

        // Try to find existing product by normalized name
        var existingProducts = await _productRepository.FindAsync(
            p => p.NormalizedName == normalizedName,
            cancellationToken);

        var existingProduct = existingProducts.FirstOrDefault();
        if (existingProduct != null)
        {
            Logger.LogDebug("Found existing product: {ProductName}", existingProduct.Name);
            return existingProduct;
        }

        // Create new product
        var newProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = productName,
            NormalizedName = normalizedName,
            CategoryId = null, // Will be assigned by categorization agent
            Frequency = PurchaseFrequency.Unknown,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        await _productRepository.AddAsync(newProduct, cancellationToken);

        Logger.LogInformation("Created new product: {ProductName}", productName);
        return newProduct;
    }

    private string NormalizeProductName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Remove extra whitespace, convert to lowercase, trim
        return System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ")
            .Trim()
            .ToLowerInvariant();
    }

    private ReceiptStatus DetermineReceiptStatus(double confidenceScore)
    {
        // Receipts with confidence > 85% are auto-verified
        if (confidenceScore >= 0.85)
            return ReceiptStatus.Verified;

        // Receipts with confidence < 70% need manual review
        if (confidenceScore < 0.70)
            return ReceiptStatus.NeedsReview;

        // Receipts with confidence 70-85% are pending verification
        return ReceiptStatus.Pending;
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
