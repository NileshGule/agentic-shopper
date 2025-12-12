using AgenticShopper.Agents.Categorization.Prompts;
using AgenticShopper.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgenticShopper.Agents.Categorization.Services;

/// <summary>
/// Result of product categorization
/// </summary>
public class CategorizationResult
{
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public bool IsManualOverride { get; set; }
}

/// <summary>
/// Service for classifying products into categories using LLM
/// </summary>
public class CategoryClassifier
{
    private readonly ILlmProvider? _llmProvider;
    private readonly ILogger _logger;

    // Predefined categories matching database seed data
    private static readonly string[] PredefinedCategories = new[]
    {
        "Fruits & Vegetables",
        "Meat & Seafood",
        "Dairy & Eggs",
        "Bakery & Bread",
        "Pantry Staples",
        "Snacks & Sweets",
        "Beverages",
        "Frozen Foods",
        "Household & Cleaning",
        "Personal Care",
        "Other"
    };

    public CategoryClassifier(
        ILlmProvider? llmProvider,
        ILogger logger)
    {
        _llmProvider = llmProvider;
        _logger = logger;
    }

    /// <summary>
    /// Classify a single product into a category
    /// </summary>
    public async Task<CategorizationResult> ClassifyProductAsync(
        string productName,
        string? storeName = null,
        string? previousCategory = null,
        int previousPurchaseCount = 0,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Classifying product: {ProductName}", productName);

        // If no LLM provider available, use fallback logic
        if (_llmProvider == null)
        {
            _logger.LogWarning("No LLM provider available, using fallback categorization");
            return FallbackCategorization(productName);
        }

        try
        {
            var userPrompt = CategorizationPrompts.BuildSingleProductPrompt(
                productName,
                storeName,
                previousCategory,
                previousPurchaseCount
            );

            var request = new LlmRequest
            {
                SystemPrompt = CategorizationPrompts.SystemPrompt,
                Prompt = userPrompt,
                Temperature = 0.3, // Lower temperature for more consistent categorization
                MaxTokens = 500
            };

            var response = await _llmProvider.GenerateAsync(request, cancellationToken);

            return ParseLlmResponse(productName, response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying product {ProductName}, using fallback", productName);
            return FallbackCategorization(productName);
        }
    }

    /// <summary>
    /// Classify multiple products in a single LLM call (more efficient)
    /// </summary>
    public async Task<IEnumerable<CategorizationResult>> ClassifyProductsBatchAsync(
        IEnumerable<string> productNames,
        CancellationToken cancellationToken = default)
    {
        var names = productNames.ToList();
        _logger.LogInformation("Batch classifying {Count} products", names.Count);

        if (_llmProvider == null)
        {
            _logger.LogWarning("No LLM provider available, using fallback categorization");
            return names.Select(FallbackCategorization);
        }

        try
        {
            var userPrompt = CategorizationPrompts.BuildBatchProductPrompt(names);

            var request = new LlmRequest
            {
                SystemPrompt = CategorizationPrompts.SystemPrompt,
                Prompt = userPrompt,
                Temperature = 0.3,
                MaxTokens = 2000
            };

            var response = await _llmProvider.GenerateAsync(request, cancellationToken);

            return ParseBatchLlmResponse(response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch classification, using fallback");
            return names.Select(FallbackCategorization);
        }
    }

    /// <summary>
    /// Validate an existing categorization
    /// </summary>
    public async Task<CategorizationResult> ValidateCategorizationAsync(
        string productName,
        string currentCategory,
        string validationReason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating category '{Category}' for product: {ProductName}",
            currentCategory, productName);

        if (_llmProvider == null)
        {
            return new CategorizationResult
            {
                ProductName = productName,
                Category = currentCategory,
                Confidence = 0.5,
                Reasoning = "Validation skipped - no LLM provider available"
            };
        }

        try
        {
            var userPrompt = CategorizationPrompts.BuildValidationPrompt(
                productName,
                currentCategory,
                validationReason
            );

            var request = new LlmRequest
            {
                SystemPrompt = CategorizationPrompts.SystemPrompt,
                Prompt = userPrompt,
                Temperature = 0.3,
                MaxTokens = 500
            };

            var response = await _llmProvider.GenerateAsync(request, cancellationToken);

            return ParseLlmResponse(productName, response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating categorization");
            return new CategorizationResult
            {
                ProductName = productName,
                Category = currentCategory,
                Confidence = 0.5,
                Reasoning = "Validation failed"
            };
        }
    }

    /// <summary>
    /// Parse LLM JSON response into CategorizationResult
    /// </summary>
    private CategorizationResult ParseLlmResponse(string productName, string response)
    {
        try
        {
            // Extract JSON from response (in case LLM adds extra text)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<LlmCategorizationResponse>(jsonText);

                if (parsed != null)
                {
                    return new CategorizationResult
                    {
                        ProductName = productName,
                        Category = parsed.Category,
                        Confidence = parsed.Confidence,
                        Reasoning = parsed.Reasoning
                    };
                }
            }

            _logger.LogWarning("Failed to parse LLM response, using fallback");
            return FallbackCategorization(productName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing LLM response");
            return FallbackCategorization(productName);
        }
    }

    /// <summary>
    /// Parse batch LLM response
    /// </summary>
    private IEnumerable<CategorizationResult> ParseBatchLlmResponse(string response)
    {
        try
        {
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<List<BatchCategorizationResponse>>(jsonText);

                if (parsed != null)
                {
                    return parsed.Select(p => new CategorizationResult
                    {
                        ProductName = p.ProductName,
                        Category = p.Category,
                        Confidence = p.Confidence,
                        Reasoning = p.Reasoning
                    });
                }
            }

            _logger.LogWarning("Failed to parse batch LLM response");
            return Enumerable.Empty<CategorizationResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing batch LLM response");
            return Enumerable.Empty<CategorizationResult>();
        }
    }

    /// <summary>
    /// Fallback categorization using simple keyword matching
    /// </summary>
    private CategorizationResult FallbackCategorization(string productName)
    {
        var lower = productName.ToLowerInvariant();
        
        // Simple keyword-based categorization
        if (ContainsAny(lower, "apple", "banana", "orange", "tomato", "lettuce", "carrot", "potato"))
            return CreateResult(productName, "Fruits & Vegetables", 0.6, "Keyword match");

        if (ContainsAny(lower, "chicken", "beef", "pork", "fish", "meat", "salmon"))
            return CreateResult(productName, "Meat & Seafood", 0.6, "Keyword match");

        if (ContainsAny(lower, "milk", "cheese", "yogurt", "egg", "butter", "cream"))
            return CreateResult(productName, "Dairy & Eggs", 0.6, "Keyword match");

        if (ContainsAny(lower, "bread", "bun", "bagel", "croissant", "pastry"))
            return CreateResult(productName, "Bakery & Bread", 0.6, "Keyword match");

        if (ContainsAny(lower, "rice", "pasta", "flour", "sugar", "salt", "oil", "sauce"))
            return CreateResult(productName, "Pantry Staples", 0.6, "Keyword match");

        if (ContainsAny(lower, "chip", "cookie", "candy", "chocolate", "snack"))
            return CreateResult(productName, "Snacks & Sweets", 0.6, "Keyword match");

        if (ContainsAny(lower, "juice", "soda", "water", "coffee", "tea", "drink"))
            return CreateResult(productName, "Beverages", 0.6, "Keyword match");

        if (ContainsAny(lower, "frozen", "ice cream"))
            return CreateResult(productName, "Frozen Foods", 0.6, "Keyword match");

        if (ContainsAny(lower, "soap", "detergent", "paper", "cleaner", "towel"))
            return CreateResult(productName, "Household & Cleaning", 0.6, "Keyword match");

        if (ContainsAny(lower, "shampoo", "toothpaste", "tissue", "deodorant"))
            return CreateResult(productName, "Personal Care", 0.6, "Keyword match");

        return CreateResult(productName, "Other", 0.4, "No keyword match found");
    }

    private bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private CategorizationResult CreateResult(string productName, string category, double confidence, string reasoning)
    {
        return new CategorizationResult
        {
            ProductName = productName,
            Category = category,
            Confidence = confidence,
            Reasoning = reasoning
        };
    }

    // DTOs for JSON deserialization
    private class LlmCategorizationResponse
    {
        public string Category { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
    }

    private class BatchCategorizationResponse
    {
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
    }
}
