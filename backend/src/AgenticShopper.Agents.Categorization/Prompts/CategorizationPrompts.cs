namespace AgenticShopper.Agents.Categorization.Prompts;

/// <summary>
/// Prompt templates for LLM-powered product categorization
/// </summary>
public static class CategorizationPrompts
{
    /// <summary>
    /// System prompt defining the LLM's role as a product categorization assistant
    /// </summary>
    public const string SystemPrompt = @"You are an intelligent product categorization assistant for a family shopping application. 
Your task is to categorize grocery and household products into appropriate categories to help families organize their shopping.

Available categories:
1. Fruits & Vegetables - Fresh produce, fruits, vegetables
2. Meat & Seafood - Fresh and frozen meats, poultry, fish, seafood
3. Dairy & Eggs - Milk, cheese, yogurt, eggs, butter
4. Bakery & Bread - Bread, buns, pastries, cakes, bagels
5. Pantry Staples - Rice, pasta, flour, sugar, oil, condiments
6. Snacks & Sweets - Chips, cookies, candy, chocolate, crackers
7. Beverages - Soft drinks, juice, coffee, tea, water
8. Frozen Foods - Frozen meals, ice cream, frozen vegetables
9. Household & Cleaning - Cleaning supplies, paper products, laundry detergent
10. Personal Care - Toiletries, hygiene products, cosmetics
11. Other - Items that don't fit other categories

Guidelines:
- Choose the most specific category that fits the product
- Consider the primary use/purpose of the product
- Be consistent with similar products
- If a product could fit multiple categories, choose the most common usage
- Provide a brief reason for your categorization

Respond in JSON format with:
{
  ""category"": ""Category Name"",
  ""confidence"": 0.95,
  ""reasoning"": ""Brief explanation""
}";

    /// <summary>
    /// User prompt template for single product categorization
    /// {0} = product name
    /// {1} = optional context (store name, previous purchases)
    /// </summary>
    public const string SingleProductPrompt = @"Categorize the following product:

Product Name: {0}

{1}

Please provide the category, confidence score (0.0 to 1.0), and a brief reasoning.";

    /// <summary>
    /// User prompt template for batch product categorization
    /// {0} = JSON array of product names
    /// </summary>
    public const string BatchProductPrompt = @"Categorize the following products. Return a JSON array with categorization for each product.

Products:
{0}

For each product, provide:
{{
  ""productName"": ""name"",
  ""category"": ""Category Name"",
  ""confidence"": 0.95,
  ""reasoning"": ""Brief explanation""
}}";

    /// <summary>
    /// Context template for providing purchase history
    /// {0} = store name
    /// {1} = previous category (if exists)
    /// {2} = number of previous purchases
    /// </summary>
    public const string ContextTemplate = @"Additional context:
- Purchased from: {0}
- Previously categorized as: {1}
- Number of previous purchases: {2}";

    /// <summary>
    /// Prompt for category validation and correction
    /// {0} = product name
    /// {1} = current category
    /// {2} = reason for validation
    /// </summary>
    public const string ValidationPrompt = @"Validate or correct the categorization for this product:

Product Name: {0}
Current Category: {1}
Validation Reason: {2}

Is this categorization correct? If not, suggest a better category with reasoning.
If correct, confirm with confidence score.";

    /// <summary>
    /// Build the complete prompt for single product categorization
    /// </summary>
    public static string BuildSingleProductPrompt(
        string productName,
        string? storeName = null,
        string? previousCategory = null,
        int previousPurchaseCount = 0)
    {
        var context = string.Empty;
        
        if (!string.IsNullOrEmpty(storeName) || !string.IsNullOrEmpty(previousCategory) || previousPurchaseCount > 0)
        {
            context = string.Format(
                ContextTemplate,
                storeName ?? "Unknown",
                previousCategory ?? "None",
                previousPurchaseCount
            );
        }

        return string.Format(SingleProductPrompt, productName, context);
    }

    /// <summary>
    /// Build the complete prompt for batch product categorization
    /// </summary>
    public static string BuildBatchProductPrompt(IEnumerable<string> productNames)
    {
        var productsJson = System.Text.Json.JsonSerializer.Serialize(productNames);
        return string.Format(BatchProductPrompt, productsJson);
    }

    /// <summary>
    /// Build validation prompt for category correction
    /// </summary>
    public static string BuildValidationPrompt(
        string productName,
        string currentCategory,
        string validationReason)
    {
        return string.Format(ValidationPrompt, productName, currentCategory, validationReason);
    }
}
