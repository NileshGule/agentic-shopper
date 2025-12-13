using Microsoft.AspNetCore.SignalR;

namespace AgenticShopper.Coordinator.Hubs;

/// <summary>
/// SignalR hub for real-time shopping list collaboration (T155, T168)
/// </summary>
public class ShoppingListHub : Hub
{
    private readonly ILogger<ShoppingListHub> _logger;

    public ShoppingListHub(ILogger<ShoppingListHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join a shopping list group for real-time updates
    /// </summary>
    public async Task JoinList(string listId, string userId, string userName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, listId);
        _logger.LogInformation("User {UserId} ({UserName}) joined list {ListId}", userId, userName, listId);
        
        // Notify others (excluding the caller)
        await Clients.OthersInGroup(listId).SendAsync("UserJoined", new 
        { 
            UserId = userId, 
            UserName = userName, 
            ConnectionId = Context.ConnectionId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Leave a shopping list group
    /// </summary>
    public async Task LeaveList(string listId, string userId, string userName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, listId);
        _logger.LogInformation("User {UserId} ({UserName}) left list {ListId}", userId, userName, listId);
        
        await Clients.OthersInGroup(listId).SendAsync("UserLeft", new 
        { 
            UserId = userId, 
            UserName = userName,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify group members of an item addition
    /// </summary>
    public async Task ItemAdded(string listId, object item, string userId)
    {
        _logger.LogDebug("Item added to list {ListId} by user {UserId}", listId, userId);
        
        await Clients.OthersInGroup(listId).SendAsync("ItemAdded", new 
        { 
            Item = item, 
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify group members of an item update (T168 - last-write-wins)
    /// </summary>
    public async Task ItemUpdated(string listId, object item, string userId)
    {
        _logger.LogDebug("Item updated in list {ListId} by user {UserId}", listId, userId);
        
        // Last-write-wins: notify all others of the update
        await Clients.OthersInGroup(listId).SendAsync("ItemUpdated", new 
        { 
            Item = item, 
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify group members of an item deletion
    /// </summary>
    public async Task ItemDeleted(string listId, string itemId, string userId)
    {
        _logger.LogDebug("Item {ItemId} deleted from list {ListId} by user {UserId}", 
            itemId, listId, userId);
        
        await Clients.OthersInGroup(listId).SendAsync("ItemDeleted", new 
        { 
            ItemId = itemId, 
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify group members of an item marked as purchased
    /// </summary>
    public async Task ItemPurchased(string listId, string itemId, bool isPurchased, string userId)
    {
        _logger.LogDebug("Item {ItemId} purchase status changed to {Status} in list {ListId} by user {UserId}", 
            itemId, isPurchased, listId, userId);
        
        await Clients.OthersInGroup(listId).SendAsync("ItemPurchased", new 
        { 
            ItemId = itemId, 
            IsPurchased = isPurchased, 
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify group members of list name change (T161)
    /// </summary>
    public async Task ListRenamed(string listId, string newName, string userId)
    {
        _logger.LogInformation("List {ListId} renamed to '{NewName}' by user {UserId}", 
            listId, newName, userId);
        
        await Clients.OthersInGroup(listId).SendAsync("ListRenamed", new 
        { 
            ListId = listId, 
            NewName = newName, 
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify group members of list completion
    /// </summary>
    public async Task ListCompleted(string listId, string userId)
    {
        _logger.LogInformation("List {ListId} marked as completed by user {UserId}", listId, userId);
        
        await Clients.OthersInGroup(listId).SendAsync("ListCompleted", new 
        { 
            ListId = listId, 
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// User is actively viewing/editing an item (for presence indication)
    /// </summary>
    public async Task UserEditingItem(string listId, string itemId, string userId, string userName)
    {
        await Clients.OthersInGroup(listId).SendAsync("UserEditingItem", new 
        { 
            ItemId = itemId, 
            UserId = userId, 
            UserName = userName,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// User stopped editing an item
    /// </summary>
    public async Task UserStoppedEditing(string listId, string itemId, string userId)
    {
        await Clients.OthersInGroup(listId).SendAsync("UserStoppedEditing", new 
        { 
            ItemId = itemId, 
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Connection {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
