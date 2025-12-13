using AgenticShopper.Core.Interfaces;
using AgenticShopper.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AgenticShopper.Data.Repositories;

public class ShoppingListRepository : IRepository<ShoppingList>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShoppingListRepository> _logger;

    public ShoppingListRepository(ApplicationDbContext context, ILogger<ShoppingListRepository> _logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        this._logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
    }

    public async Task<ShoppingList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ShoppingLists
                .Include(sl => sl.Family)
                .Include(sl => sl.Creator)
                .Include(sl => sl.Items)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(p => p!.Category)
                .FirstOrDefaultAsync(sl => sl.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shopping list with ID {ListId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<ShoppingList>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ShoppingLists
                .Include(sl => sl.Family)
                .Include(sl => sl.Creator)
                .Include(sl => sl.Items)
                .OrderByDescending(sl => sl.CreatedDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all shopping lists");
            throw;
        }
    }

    public async Task<ShoppingList> AddAsync(ShoppingList entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            entity.CreatedDate = DateTime.UtcNow;
            entity.Status = ListStatus.Active;

            await _context.ShoppingLists.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added new shopping list with ID {ListId}: {ListName}", entity.Id, entity.Name);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding shopping list {ListName}", entity.Name);
            throw;
        }
    }

    public async Task<ShoppingList> UpdateAsync(ShoppingList entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            _context.ShoppingLists.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated shopping list with ID {ListId}: {ListName}", entity.Id, entity.Name);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shopping list with ID {ListId}", entity.Id);
            throw;
        }
    }

    public async Task<IEnumerable<ShoppingList>> FindAsync(
        Expression<Func<ShoppingList, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ShoppingLists
                .Include(sl => sl.Family)
                .Include(sl => sl.Creator)
                .Include(sl => sl.Items)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding shopping lists");
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var shoppingList = await _context.ShoppingLists.FindAsync([id], cancellationToken);
            if (shoppingList == null)
            {
                _logger.LogWarning("Shopping list with ID {ListId} not found for deletion", id);
                throw new InvalidOperationException($"Shopping list with ID {id} not found");
            }

            _context.ShoppingLists.Remove(shoppingList);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted shopping list with ID {ListId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting shopping list with ID {ListId}", id);
            throw;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ShoppingLists.AnyAsync(sl => sl.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of shopping list with ID {ListId}", id);
            throw;
        }
    }

    // ShoppingList-specific methods

    /// <summary>
    /// Get all shopping lists for a specific family
    /// </summary>
    public async Task<IEnumerable<ShoppingList>> GetByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ShoppingLists
                .Include(sl => sl.Creator)
                .Include(sl => sl.Items)
                    .ThenInclude(item => item.Product)
                .Where(sl => sl.FamilyId == familyId)
                .OrderByDescending(sl => sl.CreatedDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shopping lists for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Get active shopping lists (not completed or archived)
    /// </summary>
    public async Task<IEnumerable<ShoppingList>> GetActiveListsAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ShoppingLists
                .Include(sl => sl.Creator)
                .Include(sl => sl.Items)
                    .ThenInclude(item => item.Product)
                .Where(sl => sl.FamilyId == familyId && 
                            (sl.Status == ListStatus.Active || sl.Status == ListStatus.InProgress))
                .OrderByDescending(sl => sl.CreatedDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active shopping lists for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Get shopping lists by status
    /// </summary>
    public async Task<IEnumerable<ShoppingList>> GetByStatusAsync(
        Guid familyId,
        ListStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ShoppingLists
                .Include(sl => sl.Creator)
                .Include(sl => sl.Items)
                .Where(sl => sl.FamilyId == familyId && sl.Status == status)
                .OrderByDescending(sl => sl.CreatedDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shopping lists with status {Status} for family {FamilyId}", 
                status, familyId);
            throw;
        }
    }

    /// <summary>
    /// Mark a shopping list as completed
    /// </summary>
    public async Task<ShoppingList> CompleteAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        try
        {
            var list = await GetByIdAsync(listId, cancellationToken);
            if (list == null)
            {
                throw new InvalidOperationException($"Shopping list with ID {listId} not found");
            }

            list.Status = ListStatus.Completed;
            list.CompletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Completed shopping list with ID {ListId}", listId);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing shopping list with ID {ListId}", listId);
            throw;
        }
    }

    /// <summary>
    /// Archive old completed lists
    /// </summary>
    public async Task<int> ArchiveOldListsAsync(
        Guid familyId,
        int daysOld = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            
            var listsToArchive = await _context.ShoppingLists
                .Where(sl => sl.FamilyId == familyId && 
                            sl.Status == ListStatus.Completed && 
                            sl.CompletedDate < cutoffDate)
                .ToListAsync(cancellationToken);

            foreach (var list in listsToArchive)
            {
                list.Status = ListStatus.Archived;
            }

            var archived = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Archived {Count} shopping lists for family {FamilyId}", 
                listsToArchive.Count, familyId);
            
            return listsToArchive.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving old shopping lists for family {FamilyId}", familyId);
            throw;
        }
    }

    /// <summary>
    /// Archive a specific shopping list (T154)
    /// </summary>
    public async Task<ShoppingList> ArchiveAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        try
        {
            var list = await _context.ShoppingLists.FindAsync([listId], cancellationToken);
            
            if (list == null)
            {
                throw new InvalidOperationException($"Shopping list with ID {listId} not found");
            }

            list.Status = ListStatus.Archived;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Archived shopping list {ListId}", listId);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving shopping list {ListId}", listId);
            throw;
        }
    }

    /// <summary>
    /// Restore an archived shopping list (T154)
    /// </summary>
    public async Task<ShoppingList> RestoreAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        try
        {
            var list = await _context.ShoppingLists.FindAsync([listId], cancellationToken);
            
            if (list == null)
            {
                throw new InvalidOperationException($"Shopping list with ID {listId} not found");
            }

            if (list.Status != ListStatus.Archived)
            {
                throw new InvalidOperationException($"Shopping list {listId} is not archived");
            }

            list.Status = ListStatus.Active;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Restored shopping list {ListId}", listId);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring shopping list {ListId}", listId);
            throw;
        }
    }

    /// <summary>
    /// Copy an existing shopping list (T158)
    /// </summary>
    public async Task<ShoppingList> CopyAsync(
        Guid sourceListId, 
        string newName, 
        Guid copiedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceList = await _context.ShoppingLists
                .Include(sl => sl.Items)
                    .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(sl => sl.Id == sourceListId, cancellationToken);

            if (sourceList == null)
            {
                throw new InvalidOperationException($"Shopping list with ID {sourceListId} not found");
            }

            var newList = new ShoppingList
            {
                Id = Guid.NewGuid(),
                FamilyId = sourceList.FamilyId,
                Name = newName,
                CreatedBy = copiedBy,
                CreatedDate = DateTime.UtcNow,
                Status = ListStatus.Active,
                SharedWith = sourceList.SharedWith
            };

            // Copy items
            foreach (var sourceItem in sourceList.Items)
            {
                newList.Items.Add(new ShoppingListItem
                {
                    Id = Guid.NewGuid(),
                    ListId = newList.Id,
                    ProductId = sourceItem.ProductId,
                    Quantity = sourceItem.Quantity,
                    IsPurchased = false, // Reset purchase status
                    Urgency = sourceItem.Urgency,
                    AddedBy = copiedBy,
                    Source = ItemSource.Manual, // Mark as manual since it's a copy
                    AddedDate = DateTime.UtcNow
                });
            }

            _context.ShoppingLists.Add(newList);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Copied shopping list {SourceId} to new list {NewId}", 
                sourceListId, newList.Id);
            
            return newList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying shopping list {ListId}", sourceListId);
            throw;
        }
    }

    /// <summary>
    /// Add item to shopping list
    /// </summary>
    public async Task<ShoppingListItem> AddItemAsync(
        Guid listId,
        ShoppingListItem item,
        CancellationToken cancellationToken = default)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        try
        {
            item.ListId = listId;
            item.AddedDate = DateTime.UtcNow;

            await _context.ShoppingListItems.AddAsync(item, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added item to shopping list {ListId}", listId);
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to shopping list {ListId}", listId);
            throw;
        }
    }

    /// <summary>
    /// Remove item from shopping list
    /// </summary>
    public async Task RemoveItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _context.ShoppingListItems.FindAsync([itemId], cancellationToken);
            if (item == null)
            {
                _logger.LogWarning("Shopping list item with ID {ItemId} not found for deletion", itemId);
                throw new InvalidOperationException($"Shopping list item with ID {itemId} not found");
            }

            _context.ShoppingListItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed item {ItemId} from shopping list", itemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {ItemId}", itemId);
            throw;
        }
    }

    /// <summary>
    /// Mark item as purchased
    /// </summary>
    public async Task<ShoppingListItem> MarkItemPurchasedAsync(
        Guid itemId,
        bool isPurchased,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _context.ShoppingListItems.FindAsync([itemId], cancellationToken);
            if (item == null)
            {
                throw new InvalidOperationException($"Shopping list item with ID {itemId} not found");
            }

            item.IsPurchased = isPurchased;
            item.PurchasedDate = isPurchased ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked item {ItemId} as {Status}", 
                itemId, isPurchased ? "purchased" : "not purchased");
            
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase status for item {ItemId}", itemId);
            throw;
        }
    }
}
