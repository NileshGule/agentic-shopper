using AgenticShopper.Agents.ListGenerator.Services;
using AgenticShopper.Core.Abstractions;
using AgenticShopper.Core.Models;
using AgenticShopper.Data;
using AgenticShopper.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.ListGenerator;

/// <summary>
/// Request for shopping list generation
/// </summary>
public class GenerateListRequest
{
    public Guid FamilyId { get; set; }
    public bool IncludeDueItems { get; set; } = true;
    public bool IncludeOverdueItems { get; set; } = true;
    public bool IncludeUpcomingItems { get; set; } = false;
    public List<Guid> CategoryFilter { get; set; } = new();
    public string? ListName { get; set; }
    public Guid CreatedBy { get; set; }
}

/// <summary>
/// Request for product suggestions
/// </summary>
public class GetSuggestionsRequest
{
    public Guid FamilyId { get; set; }
}

/// <summary>
/// Response for generated shopping list
/// </summary>
public class GeneratedListResponse
{
    public Guid ListId { get; set; }
    public string ListName { get; set; } = string.Empty;
    public List<GeneratedListItem> Items { get; set; } = new();
    public ListMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Individual item in generated list
/// </summary>
public class GeneratedListItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public ItemUrgency Urgency { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? LastPurchased { get; set; }
    public PurchaseFrequency? Frequency { get; set; }
    public int DaysSinceLastPurchase { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Metadata about generated list
/// </summary>
public class ListMetadata
{
    public int TotalItems { get; set; }
    public int OverdueCount { get; set; }
    public int DueThisWeekCount { get; set; }
    public int UpcomingCount { get; set; }
    public List<string> CategoriesIncluded { get; set; } = new();
    public DateTime GeneratedDate { get; set; }
}

/// <summary>
/// Response for product suggestions
/// </summary>
public class ProductSuggestionsResponse
{
    public int OverdueCount { get; set; }
    public int DueThisWeekCount { get; set; }
    public int UpcomingCount { get; set; }
    public List<ProductSuggestion> Suggestions { get; set; } = new();
}

/// <summary>
/// Individual product suggestion
/// </summary>
public class ProductSuggestion
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public ItemUrgency Urgency { get; set; }
    public decimal RecommendedQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Agent responsible for generating shopping lists based on frequency analysis
/// </summary>
public class ListGeneratorAgent : AgentBase
{
    private readonly RecommendationEngine _recommendationEngine;
    private readonly UrgencyClassifier _urgencyClassifier;
    private readonly ShoppingListRepository _shoppingListRepository;
    private readonly ApplicationDbContext _dbContext;

    public override string AgentId => "list-generator-agent";
    public override string AgentName => "Shopping List Generator Agent";
    public override string Description => "Generates shopping lists based on purchase frequency analysis with urgency classification";

    public ListGeneratorAgent(
        ApplicationDbContext dbContext,
        ShoppingListRepository shoppingListRepository,
        ILogger<ListGeneratorAgent> logger,
        IChatClient? chatClient = null)
        : base(logger, chatClient)
    {
        _dbContext = dbContext;
        _shoppingListRepository = shoppingListRepository;
        _recommendationEngine = new RecommendationEngine(dbContext, logger);
        _urgencyClassifier = new UrgencyClassifier(logger);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Initializing ListGeneratorAgent");
        await base.InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// Execute list generation or suggestions
    /// </summary>
    protected override async Task<AgentResult<TOutput>> OnExecuteAsync<TInput, TOutput>(
        TInput input,
        CancellationToken cancellationToken = default)
    {
        if (input is GenerateListRequest generateRequest && typeof(TOutput) == typeof(GeneratedListResponse))
        {
            var result = await GenerateListAsync(generateRequest, cancellationToken);
            return (AgentResult<TOutput>)(object)result;
        }
        else if (input is GetSuggestionsRequest suggestionsRequest && typeof(TOutput) == typeof(ProductSuggestionsResponse))
        {
            var result = await GetSuggestionsAsync(suggestionsRequest, cancellationToken);
            return (AgentResult<TOutput>)(object)result;
        }

        return AgentResult<TOutput>.Failure("Invalid request type");
    }

    /// <summary>
    /// Generate a shopping list based on frequency analysis
    /// </summary>
    private async Task<AgentResult<GeneratedListResponse>> GenerateListAsync(
        GenerateListRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Generating shopping list for family {FamilyId}", request.FamilyId);

            // Get product recommendations based on frequency
            var recommendations = await _recommendationEngine.GetRecommendationsAsync(
                request.FamilyId,
                request.IncludeDueItems,
                request.IncludeOverdueItems,
                request.IncludeUpcomingItems,
                request.CategoryFilter,
                cancellationToken);

            if (!recommendations.Any())
            {
                Logger.LogInformation("No recommendations found for family {FamilyId}", request.FamilyId);
                return AgentResult<GeneratedListResponse>.Success(new GeneratedListResponse
                {
                    ListName = request.ListName ?? $"Shopping List - {DateTime.UtcNow:yyyy-MM-dd}",
                    Metadata = new ListMetadata { GeneratedDate = DateTime.UtcNow }
                });
            }

            // Create shopping list entity
            var shoppingList = new ShoppingList
            {
                Id = Guid.NewGuid(),
                FamilyId = request.FamilyId,
                Name = request.ListName ?? $"Shopping List - {DateTime.UtcNow:yyyy-MM-dd}",
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                Status = ListStatus.Active
            };

            // Create list items from recommendations
            var listItems = new List<GeneratedListItem>();
            var categoriesIncluded = new HashSet<string>();

            foreach (var recommendation in recommendations)
            {
                var urgency = _urgencyClassifier.ClassifyUrgency(
                    recommendation.DaysSinceLastPurchase,
                    recommendation.AverageDaysBetweenPurchases,
                    recommendation.Frequency);

                var listItem = new ShoppingListItem
                {
                    Id = Guid.NewGuid(),
                    ListId = shoppingList.Id,
                    ProductId = recommendation.ProductId,
                    Quantity = 1,
                    IsPurchased = false,
                    Urgency = urgency,
                    AddedBy = request.CreatedBy,
                    Source = ItemSource.AutoGenerated,
                    AddedDate = DateTime.UtcNow
                };

                shoppingList.Items.Add(listItem);

                listItems.Add(new GeneratedListItem
                {
                    ProductId = recommendation.ProductId,
                    ProductName = recommendation.ProductName,
                    CategoryName = recommendation.CategoryName,
                    Urgency = urgency,
                    Quantity = 1,
                    LastPurchased = recommendation.LastPurchased,
                    Frequency = recommendation.Frequency,
                    DaysSinceLastPurchase = recommendation.DaysSinceLastPurchase,
                    Notes = recommendation.Notes
                });

                if (!string.IsNullOrEmpty(recommendation.CategoryName))
                {
                    categoriesIncluded.Add(recommendation.CategoryName);
                }
            }

            // Save shopping list to database
            await _shoppingListRepository.AddAsync(shoppingList, cancellationToken);

            // Create metadata
            var metadata = new ListMetadata
            {
                TotalItems = listItems.Count,
                OverdueCount = listItems.Count(i => i.Urgency == ItemUrgency.Overdue),
                DueThisWeekCount = listItems.Count(i => i.Urgency == ItemUrgency.DueThisWeek),
                UpcomingCount = listItems.Count(i => i.Urgency == ItemUrgency.Upcoming),
                CategoriesIncluded = categoriesIncluded.ToList(),
                GeneratedDate = DateTime.UtcNow
            };

            var response = new GeneratedListResponse
            {
                ListId = shoppingList.Id,
                ListName = shoppingList.Name,
                Items = listItems.OrderByDescending(i => i.Urgency)
                                .ThenBy(i => i.CategoryName)
                                .ToList(),
                Metadata = metadata
            };

            Logger.LogInformation(
                "Generated shopping list {ListId} with {ItemCount} items for family {FamilyId}",
                shoppingList.Id, listItems.Count, request.FamilyId);

            return AgentResult<GeneratedListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating shopping list for family {FamilyId}", request.FamilyId);
            return AgentResult<GeneratedListResponse>.Failure($"Failed to generate shopping list: {ex.Message}");
        }
    }

    /// <summary>
    /// Get product suggestions without creating a list
    /// </summary>
    private async Task<AgentResult<ProductSuggestionsResponse>> GetSuggestionsAsync(
        GetSuggestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Getting product suggestions for family {FamilyId}", request.FamilyId);

            // Get all recommendations (due + overdue + upcoming)
            var recommendations = await _recommendationEngine.GetRecommendationsAsync(
                request.FamilyId,
                includeDueItems: true,
                includeOverdueItems: true,
                includeUpcomingItems: true,
                categoryFilter: new List<Guid>(),
                cancellationToken);

            var suggestions = new List<ProductSuggestion>();
            int overdueCount = 0;
            int dueThisWeekCount = 0;
            int upcomingCount = 0;

            foreach (var recommendation in recommendations)
            {
                var urgency = _urgencyClassifier.ClassifyUrgency(
                    recommendation.DaysSinceLastPurchase,
                    recommendation.AverageDaysBetweenPurchases,
                    recommendation.Frequency);

                // Count by urgency
                switch (urgency)
                {
                    case ItemUrgency.Overdue:
                        overdueCount++;
                        break;
                    case ItemUrgency.DueThisWeek:
                        dueThisWeekCount++;
                        break;
                    case ItemUrgency.Upcoming:
                        upcomingCount++;
                        break;
                }

                var reason = BuildSuggestionReason(
                    recommendation.DaysSinceLastPurchase,
                    recommendation.AverageDaysBetweenPurchases,
                    recommendation.Frequency,
                    urgency);

                suggestions.Add(new ProductSuggestion
                {
                    ProductId = recommendation.ProductId,
                    ProductName = recommendation.ProductName,
                    CategoryName = recommendation.CategoryName,
                    Urgency = urgency,
                    RecommendedQuantity = 1,
                    Reason = reason
                });
            }

            var response = new ProductSuggestionsResponse
            {
                OverdueCount = overdueCount,
                DueThisWeekCount = dueThisWeekCount,
                UpcomingCount = upcomingCount,
                Suggestions = suggestions.OrderByDescending(s => s.Urgency)
                                       .ThenBy(s => s.CategoryName)
                                       .ToList()
            };

            Logger.LogInformation(
                "Generated {Count} suggestions for family {FamilyId} (Overdue: {Overdue}, Due: {Due}, Upcoming: {Upcoming})",
                suggestions.Count, request.FamilyId, overdueCount, dueThisWeekCount, upcomingCount);

            return AgentResult<ProductSuggestionsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting suggestions for family {FamilyId}", request.FamilyId);
            return AgentResult<ProductSuggestionsResponse>.Failure($"Failed to get suggestions: {ex.Message}");
        }
    }

    /// <summary>
    /// Build human-readable reason for product suggestion
    /// </summary>
    private string BuildSuggestionReason(
        int daysSinceLastPurchase,
        double averageDaysBetweenPurchases,
        PurchaseFrequency frequency,
        ItemUrgency urgency)
    {
        var frequencyText = frequency switch
        {
            PurchaseFrequency.Weekly => "weekly",
            PurchaseFrequency.Fortnightly => "fortnightly",
            PurchaseFrequency.Monthly => "monthly",
            PurchaseFrequency.Quarterly => "quarterly",
            PurchaseFrequency.Annually => "annually",
            _ => "occasionally"
        };

        var urgencyText = urgency switch
        {
            ItemUrgency.Overdue => "overdue",
            ItemUrgency.DueThisWeek => "due this week",
            ItemUrgency.Upcoming => "upcoming",
            _ => ""
        };

        return $"Last purchased {daysSinceLastPurchase} days ago, typically purchased {frequencyText} ({urgencyText})";
    }
}
