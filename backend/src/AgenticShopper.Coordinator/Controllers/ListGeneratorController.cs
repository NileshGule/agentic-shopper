using AgenticShopper.Agents.ListGenerator;
using AgenticShopper.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AgenticShopper.Coordinator.Controllers;

/// <summary>
/// REST API controller for shopping list generation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ListGeneratorController : ControllerBase
{
    private readonly ListGeneratorAgent _listGeneratorAgent;
    private readonly ShoppingListRepository _shoppingListRepository;
    private readonly ILogger<ListGeneratorController> _logger;

    public ListGeneratorController(
        ListGeneratorAgent listGeneratorAgent,
        ShoppingListRepository shoppingListRepository,
        ILogger<ListGeneratorController> logger)
    {
        _listGeneratorAgent = listGeneratorAgent;
        _shoppingListRepository = shoppingListRepository;
        _logger = logger;
    }

    /// <summary>
    /// Generate shopping list based on purchase frequencies (FR-017)
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<GeneratedListResponse>> GenerateList(
        [FromBody] GenerateListDto dto)
    {
        try
        {
            _logger.LogInformation("Generating shopping list for family {FamilyId}", dto.FamilyId);

            if (dto.FamilyId == Guid.Empty)
            {
                return BadRequest(new { error = "FamilyId is required" });
            }

            var request = new GenerateListRequest
            {
                FamilyId = dto.FamilyId,
                IncludeDueItems = dto.IncludeDueItems,
                IncludeOverdueItems = dto.IncludeOverdueItems,
                IncludeUpcomingItems = dto.IncludeUpcomingItems,
                CategoryFilter = dto.CategoryFilter,
                ListName = dto.ListName,
                CreatedBy = dto.CreatedBy
            };

            var result = await _listGeneratorAgent.ExecuteAsync<GenerateListRequest, GeneratedListResponse>(
                request, CancellationToken.None);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating shopping list");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get product suggestions without creating a list
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<ProductSuggestionsResponse>> GetSuggestions(
        [FromQuery] Guid familyId)
    {
        try
        {
            _logger.LogInformation("Getting suggestions for family {FamilyId}", familyId);

            if (familyId == Guid.Empty)
            {
                return BadRequest(new { error = "FamilyId is required" });
            }

            var request = new GetSuggestionsRequest
            {
                FamilyId = familyId
            };

            var result = await _listGeneratorAgent.ExecuteAsync<GetSuggestionsRequest, ProductSuggestionsResponse>(
                request, CancellationToken.None);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all shopping lists for a family
    /// </summary>
    [HttpGet("{familyId}")]
    public async Task<ActionResult<IEnumerable<Core.Models.ShoppingList>>> GetListsByFamily(Guid familyId)
    {
        try
        {
            _logger.LogInformation("Getting all lists for family {FamilyId}", familyId);

            var lists = await _shoppingListRepository.GetByFamilyAsync(familyId, CancellationToken.None);

            return Ok(lists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lists for family");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get active shopping lists for a family
    /// </summary>
    [HttpGet("{familyId}/active")]
    public async Task<ActionResult<IEnumerable<Core.Models.ShoppingList>>> GetActiveListsByFamily(Guid familyId)
    {
        try
        {
            _logger.LogInformation("Getting active lists for family {FamilyId}", familyId);

            var lists = await _shoppingListRepository.GetActiveListsAsync(familyId, CancellationToken.None);

            return Ok(lists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active lists");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific shopping list by ID
    /// </summary>
    [HttpGet("list/{listId}")]
    public async Task<ActionResult<Core.Models.ShoppingList>> GetListById(Guid listId)
    {
        try
        {
            _logger.LogInformation("Getting list {ListId}", listId);

            var list = await _shoppingListRepository.GetByIdAsync(listId, CancellationToken.None);

            if (list == null)
            {
                return NotFound(new { error = $"Shopping list {listId} not found" });
            }

            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting list");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Add item to shopping list (FR-019)
    /// </summary>
    [HttpPost("list/{listId}/items")]
    public async Task<ActionResult<Core.Models.ShoppingListItem>> AddItemToList(
        Guid listId,
        [FromBody] AddItemDto dto)
    {
        try
        {
            _logger.LogInformation("Adding item to list {ListId}", listId);

            var item = new Core.Models.ShoppingListItem
            {
                Id = Guid.NewGuid(),
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                IsPurchased = false,
                Urgency = dto.Urgency,
                AddedBy = dto.AddedBy,
                Source = Core.Models.ItemSource.Manual // Items added via API are manual
            };

            var addedItem = await _shoppingListRepository.AddItemAsync(listId, item, CancellationToken.None);

            return Ok(addedItem);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to list");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove item from shopping list
    /// </summary>
    [HttpDelete("list/items/{itemId}")]
    public async Task<ActionResult> RemoveItemFromList(Guid itemId)
    {
        try
        {
            _logger.LogInformation("Removing item {ItemId}", itemId);

            await _shoppingListRepository.RemoveItemAsync(itemId, CancellationToken.None);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Mark item as purchased/unpurchased
    /// </summary>
    [HttpPatch("list/items/{itemId}/purchase")]
    public async Task<ActionResult<Core.Models.ShoppingListItem>> UpdateItemPurchaseStatus(
        Guid itemId,
        [FromBody] UpdatePurchaseStatusDto dto)
    {
        try
        {
            _logger.LogInformation("Updating purchase status for item {ItemId} to {Status}", itemId, dto.IsPurchased);

            var item = await _shoppingListRepository.MarkItemPurchasedAsync(itemId, dto.IsPurchased, CancellationToken.None);

            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item purchase status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Complete a shopping list
    /// </summary>
    [HttpPost("list/{listId}/complete")]
    public async Task<ActionResult<Core.Models.ShoppingList>> CompleteList(Guid listId)
    {
        try
        {
            _logger.LogInformation("Completing list {ListId}", listId);

            var list = await _shoppingListRepository.CompleteAsync(listId, CancellationToken.None);

            return Ok(list);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing list");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Archive old completed lists (FR-020)
    /// </summary>
    [HttpPost("{familyId}/archive")]
    public async Task<ActionResult<ArchiveResultDto>> ArchiveOldLists(
        Guid familyId,
        [FromQuery] int daysOld = 30)
    {
        try
        {
            _logger.LogInformation("Archiving old lists for family {FamilyId} (older than {Days} days)", 
                familyId, daysOld);

            var archivedCount = await _shoppingListRepository.ArchiveOldListsAsync(
                familyId, daysOld, CancellationToken.None);

            return Ok(new ArchiveResultDto
            {
                ArchivedCount = archivedCount,
                FamilyId = familyId,
                DaysOld = daysOld
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving old lists");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Archive a specific shopping list (T159)
    /// </summary>
    [HttpPost("{listId}/archive")]
    public async Task<ActionResult<ShoppingList>> ArchiveList(Guid listId)
    {
        try
        {
            _logger.LogInformation("Archiving shopping list {ListId}", listId);

            var list = await _shoppingListRepository.ArchiveAsync(listId, CancellationToken.None);
            return Ok(list);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving list {ListId}", listId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Restore an archived shopping list (T159)
    /// </summary>
    [HttpPost("{listId}/restore")]
    public async Task<ActionResult<ShoppingList>> RestoreList(Guid listId)
    {
        try
        {
            _logger.LogInformation("Restoring shopping list {ListId}", listId);

            var list = await _shoppingListRepository.RestoreAsync(listId, CancellationToken.None);
            return Ok(list);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring list {ListId}", listId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Copy an existing shopping list (T158)
    /// </summary>
    [HttpPost("{listId}/copy")]
    public async Task<ActionResult<ShoppingList>> CopyList(
        Guid listId,
        [FromBody] CopyListDto dto)
    {
        try
        {
            _logger.LogInformation("Copying shopping list {ListId}", listId);

            if (string.IsNullOrWhiteSpace(dto.NewName))
            {
                return BadRequest(new { error = "NewName is required" });
            }

            var newList = await _shoppingListRepository.CopyAsync(
                listId, dto.NewName, dto.CopiedBy, CancellationToken.None);
            
            return Ok(newList);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying list {ListId}", listId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// DTO for generating shopping list
/// </summary>
public class GenerateListDto
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
/// DTO for adding item to list
/// </summary>
public class AddItemDto
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public Core.Models.ItemUrgency Urgency { get; set; }
    public Guid AddedBy { get; set; }
}

/// <summary>
/// DTO for updating item purchase status
/// </summary>
public class UpdatePurchaseStatusDto
{
    public bool IsPurchased { get; set; }
}

/// <summary>
/// DTO for archive operation result
/// </summary>
public class ArchiveResultDto
{
    public int ArchivedCount { get; set; }
    public Guid FamilyId { get; set; }
    public int DaysOld { get; set; }
}

/// <summary>
/// DTO for copy list operation (T158)
/// </summary>
public class CopyListDto
{
    public required string NewName { get; set; }
    public Guid CopiedBy { get; set; }
}
