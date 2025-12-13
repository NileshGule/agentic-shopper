import * as signalR from "@microsoft/signalr";

const SIGNALR_HUB_URL =
  import.meta.env.VITE_SIGNALR_HUB_URL || "http://localhost:5000/hubs/shopping-list";

// Event payload interfaces matching backend hub
interface UserInfo {
  userId: string;
  userName: string;
  connectionId?: string;
  timestamp?: string;
}

interface ItemEvent {
  item: any;
  userId: string;
  timestamp: string;
}

interface ListEvent {
  listId: string;
  newName?: string;
  userId: string;
  timestamp: string;
}

interface EditingEvent {
  itemId: string;
  userId: string;
  userName: string;
  timestamp: string;
}

class RealtimeSyncService {
  private connection: signalR.HubConnection | null = null;
  private listId: string | null = null;
  private userId: string | null = null;
  private userName: string | null = null;

  async connect(): Promise<void> {
    if (this.connection) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(SIGNALR_HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    try {
      await this.connection.start();
      console.log("SignalR connected successfully");
    } catch (error) {
      console.error("SignalR connection error:", error);
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      if (this.listId) {
        await this.leaveList();
      }
      await this.connection.stop();
      this.connection = null;
      this.listId = null;
      this.userId = null;
      this.userName = null;
    }
  }

  async joinList(listId: string, userId: string, userName: string): Promise<void> {
    if (!this.connection) {
      await this.connect();
    }

    this.listId = listId;
    this.userId = userId;
    this.userName = userName;
    
    await this.connection!.invoke("JoinList", listId, userId, userName);
  }

  async leaveList(): Promise<void> {
    if (this.connection && this.listId && this.userId && this.userName) {
      await this.connection.invoke("LeaveList", this.listId, this.userId, this.userName);
      this.listId = null;
      this.userId = null;
      this.userName = null;
    }
  }

  // Event listeners
  onUserJoined(callback: (userInfo: UserInfo) => void): void {
    this.connection?.on("UserJoined", callback);
  }

  onUserLeft(callback: (userInfo: UserInfo) => void): void {
    this.connection?.on("UserLeft", callback);
  }

  onItemAdded(callback: (event: ItemEvent) => void): void {
    this.connection?.on("ItemAdded", callback);
  }

  onItemUpdated(callback: (event: ItemEvent) => void): void {
    this.connection?.on("ItemUpdated", callback);
  }

  onItemDeleted(callback: (event: { itemId: string; userId: string; timestamp: string }) => void): void {
    this.connection?.on("ItemDeleted", callback);
  }

  onItemPurchased(callback: (event: { itemId: string; isPurchased: boolean; userId: string; timestamp: string }) => void): void {
    this.connection?.on("ItemPurchased", callback);
  }

  onListRenamed(callback: (event: ListEvent) => void): void {
    this.connection?.on("ListRenamed", callback);
  }

  onListCompleted(callback: (event: ListEvent) => void): void {
    this.connection?.on("ListCompleted", callback);
  }

  onUserEditingItem(callback: (event: EditingEvent) => void): void {
    this.connection?.on("UserEditingItem", callback);
  }

  onUserStoppedEditing(callback: (event: EditingEvent) => void): void {
    this.connection?.on("UserStoppedEditing", callback);
  }

  // Notifications to send to other users
  async notifyItemAdded(listId: string, item: any, userId: string): Promise<void> {
    await this.connection?.invoke("ItemAdded", listId, item, userId);
  }

  async notifyItemUpdated(listId: string, item: any, userId: string): Promise<void> {
    await this.connection?.invoke("ItemUpdated", listId, item, userId);
  }

  async notifyItemDeleted(listId: string, itemId: string, userId: string): Promise<void> {
    await this.connection?.invoke("ItemDeleted", listId, itemId, userId);
  }

  async notifyItemPurchased(listId: string, itemId: string, isPurchased: boolean, userId: string): Promise<void> {
    await this.connection?.invoke("ItemPurchased", listId, itemId, isPurchased, userId);
  }

  async notifyListRenamed(listId: string, newName: string, userId: string): Promise<void> {
    await this.connection?.invoke("ListRenamed", listId, newName, userId);
  }

  async notifyListCompleted(listId: string, userId: string): Promise<void> {
    await this.connection?.invoke("ListCompleted", listId, userId);
  }

  async notifyUserEditingItem(listId: string, itemId: string, userId: string, userName: string): Promise<void> {
    await this.connection?.invoke("UserEditingItem", listId, itemId, userId, userName);
  }

  async notifyUserStoppedEditing(listId: string, itemId: string, userId: string, userName: string): Promise<void> {
    await this.connection?.invoke("UserStoppedEditing", listId, itemId, userId, userName);
  }

  // Remove old event listeners to prevent memory leaks
  removeAllListeners(): void {
    this.connection?.off("UserJoined");
    this.connection?.off("UserLeft");
    this.connection?.off("ItemAdded");
    this.connection?.off("ItemUpdated");
    this.connection?.off("ItemDeleted");
    this.connection?.off("ItemPurchased");
    this.connection?.off("ListRenamed");
    this.connection?.off("ListCompleted");
    this.connection?.off("UserEditingItem");
    this.connection?.off("UserStoppedEditing");
  }
}

export default new RealtimeSyncService();

