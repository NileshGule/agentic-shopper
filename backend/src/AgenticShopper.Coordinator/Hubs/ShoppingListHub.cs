using Microsoft.AspNetCore.SignalR;

namespace AgenticShopper.Coordinator.Hubs;

/// <summary>
/// SignalR hub for real-time shopping list collaboration
/// </summary>
public class ShoppingListHub : Hub
{
    /// <summary>
    /// Join a shopping list group for real-time updates
    /// </summary>
    public async Task JoinList(string listId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, listId);
        await Clients.Group(listId).SendAsync("UserJoined", Context.ConnectionId);
    }

    /// <summary>
    /// Leave a shopping list group
    /// </summary>
    public async Task LeaveList(string listId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, listId);
        await Clients.Group(listId).SendAsync("UserLeft", Context.ConnectionId);
    }

    /// <summary>
    /// Notify group members of an item addition
    /// </summary>
    public async Task ItemAdded(string listId, object item)
    {
        await Clients.Group(listId).SendAsync("ItemAdded", item);
    }

    /// <summary>
    /// Notify group members of an item update
    /// </summary>
    public async Task ItemUpdated(string listId, object item)
    {
        await Clients.Group(listId).SendAsync("ItemUpdated", item);
    }

    /// <summary>
    /// Notify group members of an item deletion
    /// </summary>
    public async Task ItemDeleted(string listId, string itemId)
    {
        await Clients.Group(listId).SendAsync("ItemDeleted", itemId);
    }

    /// <summary>
    /// Notify group members of an item marked as purchased
    /// </summary>
    public async Task ItemPurchased(string listId, string itemId, bool isPurchased)
    {
        await Clients.Group(listId).SendAsync("ItemPurchased", itemId, isPurchased);
    }
}
