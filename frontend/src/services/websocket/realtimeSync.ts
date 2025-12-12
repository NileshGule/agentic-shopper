import * as signalR from "@microsoft/signalr";

const SIGNALR_HUB_URL =
  import.meta.env.VITE_SIGNALR_HUB_URL || "http://localhost:5000/hubs/shopping-list";

class RealtimeSyncService {
  private connection: signalR.HubConnection | null = null;
  private listId: string | null = null;

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
      await this.connection.stop();
      this.connection = null;
      this.listId = null;
    }
  }

  async joinList(listId: string): Promise<void> {
    if (!this.connection) {
      await this.connect();
    }

    this.listId = listId;
    await this.connection!.invoke("JoinList", listId);
  }

  async leaveList(): Promise<void> {
    if (this.connection && this.listId) {
      await this.connection.invoke("LeaveList", this.listId);
      this.listId = null;
    }
  }

  onItemAdded(callback: (item: any) => void): void {
    this.connection?.on("ItemAdded", callback);
  }

  onItemUpdated(callback: (item: any) => void): void {
    this.connection?.on("ItemUpdated", callback);
  }

  onItemDeleted(callback: (itemId: string) => void): void {
    this.connection?.on("ItemDeleted", callback);
  }

  onItemPurchased(callback: (itemId: string, isPurchased: boolean) => void): void {
    this.connection?.on("ItemPurchased", callback);
  }

  async notifyItemAdded(listId: string, item: any): Promise<void> {
    await this.connection?.invoke("ItemAdded", listId, item);
  }

  async notifyItemUpdated(listId: string, item: any): Promise<void> {
    await this.connection?.invoke("ItemUpdated", listId, item);
  }

  async notifyItemDeleted(listId: string, itemId: string): Promise<void> {
    await this.connection?.invoke("ItemDeleted", listId, itemId);
  }

  async notifyItemPurchased(listId: string, itemId: string, isPurchased: boolean): Promise<void> {
    await this.connection?.invoke("ItemPurchased", listId, itemId, isPurchased);
  }
}

export default new RealtimeSyncService();
