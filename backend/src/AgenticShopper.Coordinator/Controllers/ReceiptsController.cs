using AgenticShopper.Agents.Receipt;
using AgenticShopper.Agents.Frequency;
using AgenticShopper.Agents.Budget;
using AgenticShopper.Coordinator.DTOs;
using AgenticShopper.Core.Interfaces;
using AgenticShopper.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AgenticShopper.Coordinator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReceiptsController : ControllerBase
{
    private readonly ILogger<ReceiptsController> _logger;
    private readonly ReceiptAgent _receiptAgent;
    private readonly IRepository<Receipt> _receiptRepository;
    private readonly FrequencyAgent _frequencyAgent;
    private readonly BudgetAgent? _budgetAgent;
    private readonly IServiceScopeFactory _scopeFactory;

    public ReceiptsController(
        ILogger<ReceiptsController> logger,
        ReceiptAgent receiptAgent,
        IRepository<Receipt> receiptRepository,
        FrequencyAgent frequencyAgent,
        IServiceScopeFactory scopeFactory,
        BudgetAgent? budgetAgent = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _receiptAgent = receiptAgent ?? throw new ArgumentNullException(nameof(receiptAgent));
        _receiptRepository = receiptRepository ?? throw new ArgumentNullException(nameof(receiptRepository));
        _frequencyAgent = frequencyAgent ?? throw new ArgumentNullException(nameof(frequencyAgent));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _budgetAgent = budgetAgent; // Optional for backward compatibility
    }

    /// <summary>
    /// Upload a new receipt image for processing
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadReceiptResponse>> UploadReceipt(
        [FromForm] IFormFile file,
        [FromForm] Guid familyId,
        [FromForm] Guid uploadedBy,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (familyId == Guid.Empty)
            {
                return BadRequest(new { error = "FamilyId is required" });
            }

            if (uploadedBy == Guid.Empty)
            {
                return BadRequest(new { error = "UploadedBy is required" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { error = $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}" });
            }

            // Validate file size (max 10MB)
            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { error = "File size exceeds 10MB limit" });
            }

            _logger.LogInformation(
                "Processing receipt upload: {FileName} ({FileSize} bytes) for family {FamilyId}",
                file.FileName,
                file.Length,
                familyId);

            // Process receipt using ReceiptAgent
            using var stream = file.OpenReadStream();
            var request = new ReceiptProcessingRequest
            {
                ImageStream = stream,
                FileName = file.FileName,
                FamilyId = familyId,
                UploadedBy = uploadedBy
            };

            var result = await _receiptAgent.ExecuteAsync<ReceiptProcessingRequest, ReceiptProcessingResult>(
                request,
                cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogError("Receipt processing failed: {ErrorMessage}", result.ErrorMessage);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = result.ErrorMessage ?? "Receipt processing failed" });
            }

            var response = new UploadReceiptResponse
            {
                ReceiptId = result.Data.ReceiptId,
                StoreName = result.Data.StoreName,
                PurchaseDate = result.Data.PurchaseDate,
                TotalAmount = result.Data.TotalAmount,
                ItemCount = result.Data.ItemCount,
                ConfidenceScore = result.Data.ConfidenceScore,
                Status = result.Data.Status,
                NeedsReview = result.Data.NeedsReview,
                Message = result.Data.NeedsReview
                    ? "Receipt uploaded successfully but needs manual review due to low OCR confidence"
                    : "Receipt uploaded and processed successfully"
            };

            _logger.LogInformation(
                "Receipt {ReceiptId} uploaded successfully, status: {Status}",
                response.ReceiptId,
                response.Status);

            // T085 [FR-014]: Trigger frequency recalculation for products in this receipt
            // This happens asynchronously after receipt upload to update purchase frequency patterns
            var receiptId = result.Data.ReceiptId;
            var itemCount = result.Data.ItemCount;
            _ = Task.Run(async () =>
            {
                // Create a new DI scope so that scoped services (DbContext, repositories)
                // are not disposed when the HTTP request ends.
                using var scope = _scopeFactory.CreateScope();
                var receiptRepo = scope.ServiceProvider.GetRequiredService<IRepository<Receipt>>();
                var freqAgent = scope.ServiceProvider.GetRequiredService<FrequencyAgent>();

                try
                {
                    _logger.LogInformation(
                        "Starting frequency recalculation for receipt {ReceiptId} with {ItemCount} items",
                        receiptId,
                        itemCount);

                    // Get the full receipt with purchases
                    var receiptWithPurchases = await receiptRepo.GetByIdAsync(
                        receiptId,
                        CancellationToken.None);

                    if (receiptWithPurchases?.Purchases == null || !receiptWithPurchases.Purchases.Any())
                    {
                        _logger.LogWarning(
                            "No purchases found for receipt {ReceiptId}, skipping frequency recalculation",
                            receiptId);
                        return;
                    }

                    // Extract unique product IDs from purchases
                    var productIds = receiptWithPurchases.Purchases
                        .Select(p => p.ProductId)
                        .Distinct()
                        .ToList();

                    _logger.LogInformation(
                        "Recalculating frequency for {ProductCount} unique products from receipt {ReceiptId}",
                        productIds.Count,
                        receiptId);

                    // Trigger batch frequency calculation
                    var batchRequest = new BatchFrequencyRequest
                    {
                        ProductIds = productIds,
                        ForceRecalculate = true // Always recalculate when new purchase is added
                    };

                    var frequencyResult = await freqAgent.ExecuteAsync<BatchFrequencyRequest, FrequencyCalculationResponse>(
                        batchRequest,
                        CancellationToken.None);

                    if (frequencyResult.IsSuccess)
                    {
                        _logger.LogInformation(
                            "Successfully recalculated frequencies for {ProductCount} products from receipt {ReceiptId}",
                            productIds.Count,
                            receiptId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Frequency recalculation failed for receipt {ReceiptId}: {ErrorMessage}",
                            receiptId,
                            frequencyResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error during background frequency recalculation for receipt {ReceiptId}",
                        receiptId);
                }
            }, CancellationToken.None);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading receipt");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while processing the receipt" });
        }
    }

    /// <summary>
    /// Get all receipts for a family
    /// </summary>
    [HttpGet("family/{familyId}")]
    [ProducesResponseType(typeof(IEnumerable<ReceiptSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ReceiptSummaryDto>>> GetReceiptsByFamily(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching receipts for family {FamilyId}", familyId);

            var receipts = await _receiptRepository.FindAsync(
                r => r.FamilyId == familyId,
                cancellationToken);

            var summaries = receipts.Select(r => new ReceiptSummaryDto
            {
                Id = r.Id,
                StoreName = r.StoreName,
                PurchaseDate = r.PurchaseDate,
                TotalAmount = r.TotalAmount,
                ItemCount = r.Purchases?.Count ?? 0,
                ConfidenceScore = r.ConfidenceScore,
                Status = r.Status.ToString(),
                CreatedDate = r.CreatedDate,
                ImageUrl = r.ImageUrl
            })
            .OrderByDescending(r => r.PurchaseDate)
            .ToList();

            _logger.LogInformation("Found {Count} receipts for family {FamilyId}", summaries.Count, familyId);
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching receipts for family {FamilyId}", familyId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while fetching receipts" });
        }
    }

    /// <summary>
    /// Get detailed receipt information by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReceiptDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReceiptDetailDto>> GetReceiptById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching receipt {ReceiptId}", id);

            var receipt = await _receiptRepository.GetByIdAsync(id, cancellationToken);

            if (receipt == null)
            {
                _logger.LogWarning("Receipt {ReceiptId} not found", id);
                return NotFound(new { error = $"Receipt {id} not found" });
            }

            var detail = new ReceiptDetailDto
            {
                Id = receipt.Id,
                FamilyId = receipt.FamilyId,
                UploadedBy = receipt.UploadedBy,
                StoreName = receipt.StoreName,
                PurchaseDate = receipt.PurchaseDate,
                TotalAmount = receipt.TotalAmount,
                ImageUrl = receipt.ImageUrl,
                ConfidenceScore = receipt.ConfidenceScore,
                Status = receipt.Status.ToString(),
                CreatedDate = receipt.CreatedDate,
                VerifiedDate = receipt.VerifiedDate,
                Purchases = receipt.Purchases?.Select(p => new PurchaseDto
                {
                    Id = p.Id,
                    ProductId = p.ProductId,
                    ProductName = p.Product?.Name,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice,
                    TotalPrice = p.TotalPrice,
                    CategoryName = p.Product?.Category?.Name
                }).ToList() ?? new List<PurchaseDto>()
            };

            return Ok(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching receipt {ReceiptId}", id);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while fetching the receipt" });
        }
    }

    /// <summary>
    /// Update receipt details (for manual corrections)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ReceiptDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReceiptDetailDto>> UpdateReceipt(
        Guid id,
        [FromBody] UpdateReceiptRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating receipt {ReceiptId}", id);

            var receipt = await _receiptRepository.GetByIdAsync(id, cancellationToken);

            if (receipt == null)
            {
                _logger.LogWarning("Receipt {ReceiptId} not found", id);
                return NotFound(new { error = $"Receipt {id} not found" });
            }

            // Update receipt fields
            if (request.StoreName != null)
                receipt.StoreName = request.StoreName;

            if (request.PurchaseDate.HasValue)
                receipt.PurchaseDate = request.PurchaseDate.Value;

            if (request.TotalAmount.HasValue)
                receipt.TotalAmount = request.TotalAmount.Value;

            if (request.Status != null && Enum.TryParse<ReceiptStatus>(request.Status, out var status))
            {
                receipt.Status = status;
                if (status == ReceiptStatus.Verified)
                {
                    receipt.VerifiedDate = DateTime.UtcNow;
                }
            }

            // TODO: Handle purchase updates (request.Purchases)
            // This would involve updating existing purchases, adding new ones, and removing deleted ones

            await _receiptRepository.UpdateAsync(receipt, cancellationToken);

            // Update budget tracking when receipt is verified (T141)
            if (receipt.Status == ReceiptStatus.Verified && _budgetAgent != null && receipt.Purchases != null)
            {
                foreach (var purchase in receipt.Purchases)
                {
                    if (purchase.Product?.CategoryId != null)
                    {
                        try
                        {
                            await _budgetAgent.UpdateBudgetSpendingAsync(
                                receipt.FamilyId,
                                purchase.Product.CategoryId.Value,
                                purchase.TotalPrice,
                                cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            // Log but don't fail the receipt update
                            _logger.LogError(ex,
                                "Error updating budget for category {CategoryId} after receipt verification",
                                purchase.Product.CategoryId);
                        }
                    }
                }
            }

            _logger.LogInformation("Receipt {ReceiptId} updated successfully", id);

            // Return updated receipt details
            return await GetReceiptById(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating receipt {ReceiptId}", id);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the receipt" });
        }
    }

    /// <summary>
    /// Delete a receipt (FR-050)
    /// Implements soft delete - verified receipts cannot be deleted
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteReceipt(
        Guid id,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting receipt {ReceiptId} (force: {Force})", id, force);

            var receipt = await _receiptRepository.GetByIdAsync(id, cancellationToken);
            if (receipt == null)
            {
                _logger.LogWarning("Receipt {ReceiptId} not found", id);
                return NotFound(new { error = $"Receipt {id} not found" });
            }

            // FR-050: Prevent deletion of verified receipts unless force flag is set
            if (receipt.Status == ReceiptStatus.Verified && !force)
            {
                _logger.LogWarning(
                    "Attempted to delete verified receipt {ReceiptId} without force flag",
                    id);
                return BadRequest(new
                {
                    error = "Cannot delete verified receipt. Use force=true query parameter to override.",
                    receiptStatus = receipt.Status.ToString(),
                    verifiedDate = receipt.VerifiedDate
                });
            }

            // Soft delete: Update status to Archived instead of physical deletion
            // This preserves data for historical records and analysis
            receipt.Status = ReceiptStatus.Archived;
            await _receiptRepository.UpdateAsync(receipt, cancellationToken);

            _logger.LogInformation(
                "Receipt {ReceiptId} soft-deleted (archived) successfully",
                id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting receipt {ReceiptId}", id);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the receipt" });
        }
    }

    /// <summary>
    /// Permanently delete a receipt (admin only)
    /// This is a hard delete that removes the receipt from the database
    /// </summary>
    [HttpDelete("{id}/permanent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PermanentlyDeleteReceipt(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Permanently deleting receipt {ReceiptId}", id);

            var receipt = await _receiptRepository.GetByIdAsync(id, cancellationToken);
            if (receipt == null)
            {
                _logger.LogWarning("Receipt {ReceiptId} not found", id);
                return NotFound(new { error = $"Receipt {id} not found" });
            }

            // Hard delete - removes from database
            await _receiptRepository.DeleteAsync(id, cancellationToken);

            _logger.LogWarning("Receipt {ReceiptId} permanently deleted", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting receipt {ReceiptId}", id);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while permanently deleting the receipt" });
        }
    }

    /// <summary>
    /// Get receipts needing review (low OCR confidence)
    /// </summary>
    [HttpGet("family/{familyId}/needs-review")]
    [ProducesResponseType(typeof(IEnumerable<ReceiptSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ReceiptSummaryDto>>> GetReceiptsNeedingReview(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching receipts needing review for family {FamilyId}", familyId);

            var receipts = await _receiptRepository.FindAsync(
                r => r.FamilyId == familyId && r.Status == ReceiptStatus.NeedsReview,
                cancellationToken);

            var summaries = receipts.Select(r => new ReceiptSummaryDto
            {
                Id = r.Id,
                StoreName = r.StoreName,
                PurchaseDate = r.PurchaseDate,
                TotalAmount = r.TotalAmount,
                ItemCount = r.Purchases?.Count ?? 0,
                ConfidenceScore = r.ConfidenceScore,
                Status = r.Status.ToString(),
                CreatedDate = r.CreatedDate,
                ImageUrl = r.ImageUrl
            })
            .OrderByDescending(r => r.CreatedDate)
            .ToList();

            _logger.LogInformation(
                "Found {Count} receipts needing review for family {FamilyId}",
                summaries.Count,
                familyId);

            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching receipts needing review for family {FamilyId}", familyId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while fetching receipts" });
        }
    }
}

