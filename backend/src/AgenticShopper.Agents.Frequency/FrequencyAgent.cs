using AgenticShopper.Agents.Frequency.Services;
using AgenticShopper.Core.Abstractions;
using AgenticShopper.Core.Models;
using AgenticShopper.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.Frequency;

/// <summary>
/// Request for frequency calculation
/// </summary>
public class FrequencyCalculationRequest
{
    public Guid ProductId { get; set; }
    public bool ForceRecalculate { get; set; }
}

/// <summary>
/// Request for batch frequency calculation
/// </summary>
public class BatchFrequencyRequest
{
    public List<Guid> ProductIds { get; set; } = new();
    public bool ForceRecalculate { get; set; }
}

/// <summary>
/// Result of frequency calculation operation
/// </summary>
public class FrequencyCalculationResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public PurchaseFrequency Frequency { get; set; }
    public double AverageDaysBetweenPurchases { get; set; }
    public int PurchaseCount { get; set; }
    public DateTime? NextExpectedPurchase { get; set; }
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public bool WasUpdated { get; set; }
    public bool IsPaused { get; set; }
}

/// <summary>
/// Agent responsible for calculating and managing purchase frequency patterns
/// </summary>
public class FrequencyAgent : AgentBase
{
    private readonly FrequencyCalculator _calculator;
    private readonly ApplicationDbContext _dbContext;

    public override string AgentId => "frequency-agent";
    public override string AgentName => "Purchase Frequency Agent";
    public override string Description => "Calculates and tracks purchase frequency patterns for products";

    public FrequencyAgent(
        ApplicationDbContext dbContext,
        ILogger<FrequencyAgent> logger,
        IChatClient? chatClient = null)
        : base(logger, chatClient)
    {
        _calculator = new FrequencyCalculator(logger);
        _dbContext = dbContext;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Initializing FrequencyAgent");
        await base.InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// Execute frequency calculation
    /// </summary>
    protected override async Task<AgentResult<TOutput>> OnExecuteAsync<TInput, TOutput>(
        TInput input,
        CancellationToken cancellationToken = default)
    {
        if (input is FrequencyCalculationRequest request && typeof(TOutput) == typeof(FrequencyCalculationResponse))
        {
            var result = await CalculateFrequencyAsync(request, cancellationToken);
            return (AgentResult<TOutput>)(object)result;
        }
        else if (input is BatchFrequencyRequest batchRequest && typeof(TOutput) == typeof(FrequencyCalculationResponse))
        {
            var results = await CalculateBatchFrequencyAsync(batchRequest, cancellationToken);
            var response = results.FirstOrDefault() ?? new FrequencyCalculationResponse();
            var result = AgentResult<FrequencyCalculationResponse>.Success(response);
            return (AgentResult<TOutput>)(object)result;
        }

        return AgentResult<TOutput>.Failure("Invalid request type");
    }

    /// <summary>
    /// Calculate frequency for a single product
    /// </summary>
    private async Task<AgentResult<FrequencyCalculationResponse>> CalculateFrequencyAsync(
        FrequencyCalculationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Calculating frequency for product {ProductId}", request.ProductId);

            // Load product with purchases
            var product = await _dbContext.Products
                .Include(p => p.Purchases)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

            if (product == null)
            {
                return AgentResult<FrequencyCalculationResponse>.Failure("Product not found");
            }

            // Check if frequency is paused (FR-015)
            if (product.FrequencyPaused && !request.ForceRecalculate)
            {
                Logger.LogInformation("Frequency calculation paused for product {ProductName}", product.Name);
                
                return AgentResult<FrequencyCalculationResponse>.Success(new FrequencyCalculationResponse
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Frequency = product.Frequency,
                    PurchaseCount = product.Purchases.Count,
                    IsPaused = true,
                    Reasoning = "Frequency tracking is paused for this product",
                    WasUpdated = false
                });
            }

            // Get purchase dates
            var purchaseDates = product.Purchases
                .Select(p => p.Receipt.PurchaseDate)
                .OrderBy(d => d)
                .ToList();

            // Calculate frequency
            var frequencyResult = _calculator.CalculateFrequency(purchaseDates);

            // Update product if frequency changed
            var wasUpdated = false;
            if (product.Frequency != frequencyResult.Frequency || request.ForceRecalculate)
            {
                product.Frequency = frequencyResult.Frequency;
                product.LastPurchased = purchaseDates.Any() ? purchaseDates.Last() : null;
                
                await _dbContext.SaveChangesAsync(cancellationToken);
                wasUpdated = true;

                Logger.LogInformation("Updated product {ProductName} frequency to {Frequency}",
                    product.Name, frequencyResult.Frequency);
            }

            return AgentResult<FrequencyCalculationResponse>.Success(new FrequencyCalculationResponse
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Frequency = frequencyResult.Frequency,
                AverageDaysBetweenPurchases = frequencyResult.AverageDaysBetweenPurchases,
                PurchaseCount = frequencyResult.PurchaseCount,
                NextExpectedPurchase = frequencyResult.NextExpectedPurchase,
                Confidence = frequencyResult.Confidence,
                Reasoning = frequencyResult.Reasoning,
                WasUpdated = wasUpdated,
                IsPaused = false
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating frequency for product {ProductId}", request.ProductId);
            return AgentResult<FrequencyCalculationResponse>.Failure($"Frequency calculation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculate frequency for multiple products
    /// </summary>
    public async Task<List<FrequencyCalculationResponse>> CalculateBatchFrequencyAsync(
        BatchFrequencyRequest request,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Batch calculating frequency for {Count} products", request.ProductIds.Count);

        var results = new List<FrequencyCalculationResponse>();

        try
        {
            // Load all products with purchases
            var products = await _dbContext.Products
                .Include(p => p.Purchases)
                    .ThenInclude(pu => pu.Receipt)
                .Where(p => request.ProductIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var product in products)
            {
                // Skip if paused (unless force recalculate)
                if (product.FrequencyPaused && !request.ForceRecalculate)
                {
                    results.Add(new FrequencyCalculationResponse
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Frequency = product.Frequency,
                        PurchaseCount = product.Purchases.Count,
                        IsPaused = true,
                        Reasoning = "Frequency tracking is paused",
                        WasUpdated = false
                    });
                    continue;
                }

                var purchaseDates = product.Purchases
                    .Select(p => p.Receipt.PurchaseDate)
                    .OrderBy(d => d)
                    .ToList();

                var frequencyResult = _calculator.CalculateFrequency(purchaseDates);

                var wasUpdated = false;
                if (product.Frequency != frequencyResult.Frequency || request.ForceRecalculate)
                {
                    product.Frequency = frequencyResult.Frequency;
                    product.LastPurchased = purchaseDates.Any() ? purchaseDates.Last() : null;
                    wasUpdated = true;
                }

                results.Add(new FrequencyCalculationResponse
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Frequency = frequencyResult.Frequency,
                    AverageDaysBetweenPurchases = frequencyResult.AverageDaysBetweenPurchases,
                    PurchaseCount = frequencyResult.PurchaseCount,
                    NextExpectedPurchase = frequencyResult.NextExpectedPurchase,
                    Confidence = frequencyResult.Confidence,
                    Reasoning = frequencyResult.Reasoning,
                    WasUpdated = wasUpdated,
                    IsPaused = false
                });
            }

            if (results.Any(r => r.WasUpdated))
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                Logger.LogInformation("Updated {Count} product frequencies",
                    results.Count(r => r.WasUpdated));
            }

            return results;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in batch frequency calculation");
            return results;
        }
    }

    /// <summary>
    /// Pause frequency tracking for a product (FR-015)
    /// </summary>
    public async Task<AgentResult<FrequencyCalculationResponse>> PauseFrequencyAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _dbContext.Products
                .FindAsync(new object[] { productId }, cancellationToken);

            if (product == null)
            {
                return AgentResult<FrequencyCalculationResponse>.Failure("Product not found");
            }

            product.FrequencyPaused = true;
            await _dbContext.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Paused frequency tracking for product {ProductName}", product.Name);

            return AgentResult<FrequencyCalculationResponse>.Success(new FrequencyCalculationResponse
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Frequency = product.Frequency,
                IsPaused = true,
                WasUpdated = true,
                Reasoning = "Frequency tracking paused by user"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error pausing frequency");
            return AgentResult<FrequencyCalculationResponse>.Failure($"Failed to pause frequency: {ex.Message}");
        }
    }

    /// <summary>
    /// Resume frequency tracking for a product
    /// </summary>
    public async Task<AgentResult<FrequencyCalculationResponse>> ResumeFrequencyAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _dbContext.Products
                .FindAsync(new object[] { productId }, cancellationToken);

            if (product == null)
            {
                return AgentResult<FrequencyCalculationResponse>.Failure("Product not found");
            }

            product.FrequencyPaused = false;
            await _dbContext.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Resumed frequency tracking for product {ProductName}", product.Name);

            // Recalculate frequency
            var request = new FrequencyCalculationRequest
            {
                ProductId = productId,
                ForceRecalculate = true
            };

            return await CalculateFrequencyAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error resuming frequency");
            return AgentResult<FrequencyCalculationResponse>.Failure($"Failed to resume frequency: {ex.Message}");
        }
    }

    /// <summary>
    /// Manually override frequency for a product
    /// </summary>
    public async Task<AgentResult<FrequencyCalculationResponse>> ManualOverrideAsync(
        Guid productId,
        PurchaseFrequency frequency,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _dbContext.Products
                .FindAsync(new object[] { productId }, cancellationToken);

            if (product == null)
            {
                return AgentResult<FrequencyCalculationResponse>.Failure("Product not found");
            }

            product.Frequency = frequency;
            await _dbContext.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Manually set frequency for product {ProductName} to {Frequency}",
                product.Name, frequency);

            return AgentResult<FrequencyCalculationResponse>.Success(new FrequencyCalculationResponse
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Frequency = frequency,
                WasUpdated = true,
                Confidence = 1.0,
                Reasoning = "Manually set by user"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in manual frequency override");
            return AgentResult<FrequencyCalculationResponse>.Failure($"Failed to override frequency: {ex.Message}");
        }
    }

    /// <summary>
    /// Mark product as purchased outside system (FR-021)
    /// Updates last purchase date without creating a receipt
    /// </summary>
    public async Task<AgentResult<FrequencyCalculationResponse>> MarkPurchasedExternallyAsync(
        Guid productId,
        DateTime purchaseDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _dbContext.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product == null)
            {
                return AgentResult<FrequencyCalculationResponse>.Failure("Product not found");
            }

            // Update last purchase date
            product.LastPurchased = purchaseDate;

            // Recalculate frequency based on updated purchase history
            var purchases = await _dbContext.Purchases
                .Where(p => p.ProductId == productId)
                .OrderBy(p => p.PurchaseDate)
                .Select(p => p.PurchaseDate)
                .ToListAsync(cancellationToken);

            // Add the external purchase to the calculation
            purchases.Add(purchaseDate);
            purchases = purchases.OrderBy(d => d).ToList();

            var result = _calculator.CalculateFrequency(purchases);

            product.Frequency = result.Frequency;

            await _dbContext.SaveChangesAsync(cancellationToken);

            Logger.LogInformation(
                "Marked product {ProductName} as purchased externally on {Date}, recalculated frequency to {Frequency}",
                product.Name, purchaseDate, result.Frequency);

            return AgentResult<FrequencyCalculationResponse>.Success(new FrequencyCalculationResponse
            {
                ProductId = productId,
                ProductName = product.Name,
                Frequency = result.Frequency,
                AverageDaysBetweenPurchases = result.AverageDaysBetweenPurchases,
                PurchaseCount = result.PurchaseCount,
                NextExpectedPurchase = result.NextExpectedPurchase,
                Confidence = result.Confidence,
                Reasoning = $"Marked as purchased externally on {purchaseDate:yyyy-MM-dd}, frequency recalculated based on {result.PurchaseCount} purchases",
                WasUpdated = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error marking product as purchased externally");
            return AgentResult<FrequencyCalculationResponse>.Failure($"Failed to mark as purchased: {ex.Message}");
        }
    }

    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Shutting down FrequencyAgent");
        await base.ShutdownAsync(cancellationToken);
    }
}
