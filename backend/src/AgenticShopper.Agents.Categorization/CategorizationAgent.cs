using AgenticShopper.Agents.Categorization.Services;
using AgenticShopper.Core.Abstractions;
using AgenticShopper.Core.Models;
using AgenticShopper.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.Categorization;

/// <summary>
/// Request for product categorization
/// </summary>
public class CategorizationRequest
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? StoreName { get; set; }
    public bool ForceRecategorize { get; set; }
}

/// <summary>
/// Request for batch categorization
/// </summary>
public class BatchCategorizationRequest
{
    public List<Guid> ProductIds { get; set; } = new();
    public bool ForceRecategorize { get; set; }
}

/// <summary>
/// Result of categorization operation
/// </summary>
public class CategorizationResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public bool IsNewCategory { get; set; }
    public bool WasUpdated { get; set; }
}

/// <summary>
/// Agent responsible for automatic product categorization using LLM
/// </summary>
public class CategorizationAgent : AgentBase
{
    private readonly CategoryClassifier _classifier;
    private readonly ApplicationDbContext _dbContext;

    public override string AgentId => "categorization-agent";
    public override string AgentName => "Product Categorization Agent";
    public override string Description => "Automatically categorizes products using LLM-powered classification";

    public CategorizationAgent(
        ApplicationDbContext dbContext,
        ILogger<CategorizationAgent> logger,
        IChatClient? chatClient = null)
        : base(logger, chatClient)
    {
        _classifier = new CategoryClassifier(chatClient, logger);
        _dbContext = dbContext;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Initializing CategorizationAgent");
        await base.InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// Categorize a single product
    /// </summary>
    protected override async Task<AgentResult<TOutput>> OnExecuteAsync<TInput, TOutput>(
        TInput input,
        CancellationToken cancellationToken = default)
    {
        if (input is CategorizationRequest request && typeof(TOutput) == typeof(CategorizationResponse))
        {
            var result = await CategorizeSingleProductAsync(request, cancellationToken);
            return (AgentResult<TOutput>)(object)result;
        }
        else if (input is BatchCategorizationRequest batchRequest && typeof(TOutput) == typeof(CategorizationResponse))
        {
            var results = await CategorizeBatchProductsAsync(batchRequest, cancellationToken);
            var response = results.FirstOrDefault() ?? new CategorizationResponse();
            var result = AgentResult<CategorizationResponse>.Success(response);
            return (AgentResult<TOutput>)(object)result;
        }

        return AgentResult<TOutput>.Failure("Invalid request type");
    }

    /// <summary>
    /// Categorize a single product
    /// </summary>
    private async Task<AgentResult<CategorizationResponse>> CategorizeSingleProductAsync(
        CategorizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Categorizing product {ProductId}: {ProductName}",
                request.ProductId, request.ProductName);

            // Load product with current category
            var product = await _dbContext.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

            if (product == null)
            {
                return AgentResult<CategorizationResponse>.Failure("Product not found");
            }

            // Skip if already categorized and not forcing recategorization
            if (product.CategoryId != null && !request.ForceRecategorize)
            {
                Logger.LogInformation("Product already categorized as {Category}, skipping",
                    product.Category?.Name ?? "Unknown");

                return AgentResult<CategorizationResponse>.Success(new CategorizationResponse
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CategoryName = product.Category?.Name ?? "Unknown",
                    CategoryId = product.CategoryId,
                    Confidence = 1.0,
                    Reasoning = "Already categorized",
                    WasUpdated = false
                });
            }

            // Get purchase history for context
            var purchaseCount = await _dbContext.Purchases
                .CountAsync(p => p.ProductId == request.ProductId, cancellationToken);

            // Classify using LLM
            var classification = await _classifier.ClassifyProductAsync(
                request.ProductName,
                request.StoreName,
                product.Category?.Name,
                purchaseCount,
                cancellationToken
            );

            // Find or create category
            var category = await GetOrCreateCategoryAsync(
                classification.Category,
                cancellationToken
            );

            // Update product category
            var wasUpdated = false;
            if (product.CategoryId != category.Id)
            {
                product.CategoryId = category.Id;
                product.IsManualCategory = false; // Mark as auto-categorized
                await _dbContext.SaveChangesAsync(cancellationToken);
                wasUpdated = true;

                Logger.LogInformation("Updated product {ProductName} to category {Category}",
                    product.Name, category.Name);
            }

            return AgentResult<CategorizationResponse>.Success(new CategorizationResponse
            {
                ProductId = product.Id,
                ProductName = product.Name,
                CategoryName = category.Name,
                CategoryId = category.Id,
                Confidence = classification.Confidence,
                Reasoning = classification.Reasoning,
                WasUpdated = wasUpdated
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error categorizing product {ProductId}", request.ProductId);
            return AgentResult<CategorizationResponse>.Failure($"Categorization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Categorize multiple products efficiently
    /// </summary>
    public async Task<List<CategorizationResponse>> CategorizeBatchProductsAsync(
        BatchCategorizationRequest request,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Batch categorizing {Count} products", request.ProductIds.Count);

        var results = new List<CategorizationResponse>();

        try
        {
            // Load all products
            var products = await _dbContext.Products
                .Include(p => p.Category)
                .Where(p => request.ProductIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            // Filter products that need categorization
            var productsToCategorize = products
                .Where(p => p.CategoryId == null || request.ForceRecategorize)
                .ToList();

            if (productsToCategorize.Count == 0)
            {
                Logger.LogInformation("All products already categorized");
                return results;
            }

            // Batch classify using LLM
            var productNames = productsToCategorize.Select(p => p.Name).ToList();
            var classifications = await _classifier.ClassifyProductsBatchAsync(
                productNames,
                cancellationToken
            );

            var classificationsList = classifications.ToList();

            // Process each classification
            for (int i = 0; i < productsToCategorize.Count && i < classificationsList.Count; i++)
            {
                var product = productsToCategorize[i];
                var classification = classificationsList[i];

                var category = await GetOrCreateCategoryAsync(
                    classification.Category,
                    cancellationToken
                );

                var wasUpdated = false;
                if (product.CategoryId != category.Id)
                {
                    product.CategoryId = category.Id;
                    product.IsManualCategory = false;
                    wasUpdated = true;
                }

                results.Add(new CategorizationResponse
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CategoryName = category.Name,
                    CategoryId = category.Id,
                    Confidence = classification.Confidence,
                    Reasoning = classification.Reasoning,
                    WasUpdated = wasUpdated
                });
            }

            if (results.Any(r => r.WasUpdated))
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                Logger.LogInformation("Updated {Count} product categories", 
                    results.Count(r => r.WasUpdated));
            }

            return results;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in batch categorization");
            return results;
        }
    }

    /// <summary>
    /// Get existing category or create new one
    /// </summary>
    private async Task<Category> GetOrCreateCategoryAsync(
        string categoryName,
        CancellationToken cancellationToken)
    {
        // Try to find existing category (case-insensitive)
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == categoryName.ToLower(), cancellationToken);

        if (category != null)
        {
            return category;
        }

        // Create new category
        category = new Category
        {
            Id = Guid.NewGuid(),
            Name = categoryName
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Created new category: {CategoryName}", categoryName);

        return category;
    }

    /// <summary>
    /// Manually override product category (FR-009)
    /// </summary>
    public async Task<AgentResult<CategorizationResponse>> ManualOverrideAsync(
        Guid productId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _dbContext.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product == null)
            {
                return AgentResult<CategorizationResponse>.Failure("Product not found");
            }

            var category = await _dbContext.Categories
                .FindAsync(new object[] { categoryId }, cancellationToken);

            if (category == null)
            {
                return AgentResult<CategorizationResponse>.Failure("Category not found");
            }

            product.CategoryId = categoryId;
            product.IsManualCategory = true; // Mark as manually categorized

            await _dbContext.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Manually categorized product {ProductName} as {Category}",
                product.Name, category.Name);

            return AgentResult<CategorizationResponse>.Success(new CategorizationResponse
            {
                ProductId = product.Id,
                ProductName = product.Name,
                CategoryName = category.Name,
                CategoryId = category.Id,
                Confidence = 1.0,
                Reasoning = "Manual override by user",
                WasUpdated = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in manual override");
            return AgentResult<CategorizationResponse>.Failure($"Manual override failed: {ex.Message}");
        }
    }

    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Shutting down CategorizationAgent");
        await base.ShutdownAsync(cancellationToken);
    }
}
