import React, { useState, useEffect } from 'react';
import { ShoppingList, ShoppingListItem, ItemUrgency, AddItemRequest } from '../../services/api/shoppingListApi';
import priceApi, { PromotionDto } from '../../services/api/priceApi';
import realtimeSync from '../../services/websocket/realtimeSync';
import './ListEditor.css';

export interface ListEditorProps {
  list: ShoppingList;
  onUpdateItem: (itemId: string, isPurchased: boolean) => Promise<void>;
  onAddItem?: (item: AddItemRequest) => Promise<void>;
  onRemoveItem: (itemId: string) => Promise<void>;
  onCompleteList?: () => Promise<void>;
  userId: string;
  userName?: string;
  disabled?: boolean;
}

/**
 * Component for editing shopping lists
 * Features:
 * - View list items grouped by category
 * - Mark items as purchased/unpurchased
 * - Add new items manually (FR-019)
 * - Remove items
 * - Adjust quantities
 * - Urgency indicators with color coding
 * - Progress tracking
 */
export const ListEditor: React.FC<ListEditorProps> = ({
  list,
  onUpdateItem,
  onAddItem,
  onRemoveItem,
  onCompleteList,
  userId,
  userName = 'User',
  disabled = false
}) => {
  const [items, setItems] = useState<ShoppingListItem[]>(list.items || []);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [promotions, setPromotions] = useState<Map<string, PromotionDto>>(new Map());
  const [isConnected, setIsConnected] = useState(false);
  const [activeUsers, setActiveUsers] = useState<string[]>([]);
  const [draggedItem, setDraggedItem] = useState<ShoppingListItem | null>(null);
  const [dragOverItem, setDragOverItem] = useState<ShoppingListItem | null>(null);

  // Setup SignalR connection
  useEffect(() => {
    const setupSignalR = async () => {
      try {
        await realtimeSync.connect();
        await realtimeSync.joinList(list.id, userId, userName);
        setIsConnected(true);

        // Setup event listeners
        realtimeSync.onUserJoined((userInfo) => {
          console.log('User joined:', userInfo.userName);
          setActiveUsers(prev => [...new Set([...prev, userInfo.userName])]);
        });

        realtimeSync.onUserLeft((userInfo) => {
          console.log('User left:', userInfo.userName);
          setActiveUsers(prev => prev.filter(u => u !== userInfo.userName));
        });

        realtimeSync.onItemAdded((event) => {
          if (event.userId !== userId) {
            // Another user added an item
            setItems(prev => [...prev, event.item]);
          }
        });

        realtimeSync.onItemUpdated((event) => {
          if (event.userId !== userId) {
            // Another user updated an item - last write wins
            setItems(prev => prev.map(item => 
              item.id === event.item.id ? { ...item, ...event.item } : item
            ));
          }
        });

        realtimeSync.onItemDeleted((event) => {
          if (event.userId !== userId) {
            // Another user deleted an item
            setItems(prev => prev.filter(item => item.id !== event.itemId));
          }
        });

        realtimeSync.onItemPurchased((event) => {
          if (event.userId !== userId) {
            // Another user toggled purchase status
            setItems(prev => prev.map(item => 
              item.id === event.itemId 
                ? { ...item, isPurchased: event.isPurchased }
                : item
            ));
          }
        });

        realtimeSync.onListRenamed((event) => {
          console.log('List renamed:', event.newName);
          // Parent component should handle this
        });

        realtimeSync.onListCompleted((event) => {
          console.log('List completed by another user');
          // Parent component should handle this
        });

      } catch (error) {
        console.error('SignalR setup failed:', error);
        setError('Real-time sync unavailable. Changes will still be saved.');
      }
    };

    setupSignalR();

    return () => {
      realtimeSync.leaveList().catch(console.error);
      realtimeSync.removeAllListeners();
    };
  }, [list.id, userId, userName]);

  useEffect(() => {
    setItems(list.items || []);
    loadPromotions();
  }, [list.items]);

  const loadPromotions = async () => {
    try {
      const productNames = items
        .map(item => item.product?.name)
        .filter((name): name is string => name !== undefined);

      if (productNames.length === 0) return;

      // Fetch promotions for all products
      const allPromotions = await priceApi.getCurrentPromotions({
        pageSize: 100, // Get more promotions
      });

      // Create a map of product name to best promotion
      const promoMap = new Map<string, PromotionDto>();
      
      productNames.forEach(productName => {
        // Find promotions matching this product (case-insensitive)
        const matching = allPromotions.promotions.filter(p => 
          p.productName.toLowerCase().includes(productName.toLowerCase()) ||
          productName.toLowerCase().includes(p.productName.toLowerCase())
        );

        if (matching.length > 0) {
          // Get the best promotion (highest discount)
          const bestPromo = matching.reduce((best, current) => 
            current.discountPercentage > best.discountPercentage ? current : best
          );
          promoMap.set(productName, bestPromo);
        }
      });

      setPromotions(promoMap);
    } catch (err) {
      console.error('Failed to load promotions:', err);
      // Don't show error to user, promotions are optional
    }
  };

  const getPromotion = (productName?: string): PromotionDto | undefined => {
    if (!productName) return undefined;
    return promotions.get(productName);
  };

  const formatExpiryDate = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffTime = date.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays < 0) {
      return 'Expired';
    } else if (diffDays === 0) {
      return 'Expires today';
    } else if (diffDays === 1) {
      return 'Expires tomorrow';
    } else if (diffDays <= 7) {
      return `Expires in ${diffDays} days`;
    } else {
      return `Until ${date.toLocaleDateString()}`;
    }
  };

  const getUrgencyColor = (urgency: ItemUrgency): string => {
    switch (urgency) {
      case 'Overdue':
        return '#dc3545'; // Red
      case 'DueThisWeek':
        return '#ffc107'; // Yellow/Amber
      case 'Upcoming':
        return '#28a745'; // Green
      default:
        return '#6c757d'; // Gray
    }
  };

  const getUrgencyIcon = (urgency: ItemUrgency): string => {
    switch (urgency) {
      case 'Overdue':
        return '⚠️';
      case 'DueThisWeek':
        return '⏰';
      case 'Upcoming':
        return '📅';
      default:
        return '❓';
    }
  };

  const getUrgencyLabel = (urgency: ItemUrgency): string => {
    switch (urgency) {
      case 'Overdue':
        return 'Overdue';
      case 'DueThisWeek':
        return 'Due This Week';
      case 'Upcoming':
        return 'Upcoming';
      default:
        return 'Unknown';
    }
  };

  const handleTogglePurchased = async (item: ShoppingListItem) => {
    setError(null);
    const newPurchasedState = !item.isPurchased;

    // Optimistic update
    setItems(items.map(i => 
      i.id === item.id 
        ? { ...i, isPurchased: newPurchasedState }
        : i
    ));

    try {
      await onUpdateItem(item.id, newPurchasedState);
      
      // Notify other users via SignalR
      if (isConnected) {
        await realtimeSync.notifyItemPurchased(list.id, item.id, newPurchasedState, userId);
      }
    } catch (err) {
      // Revert on error
      setItems(list.items || []);
      setError(err instanceof Error ? err.message : 'Failed to update item');
    }
  };

  const handleRemoveItem = async (item: ShoppingListItem) => {
    if (!confirm(`Remove ${item.product?.name || 'this item'} from the list?`)) {
      return;
    }

    setError(null);
    
    // Optimistic update
    setItems(items.filter(i => i.id !== item.id));

    try {
      await onRemoveItem(item.id);
      
      // Notify other users via SignalR
      if (isConnected) {
        await realtimeSync.notifyItemDeleted(list.id, item.id, userId);
      }
    } catch (err) {
      // Revert on error
      setItems(list.items || []);
      setError(err instanceof Error ? err.message : 'Failed to remove item');
    }
  };

  const handleCompleteList = async () => {
    if (!onCompleteList) return;

    const purchasedCount = items.filter(i => i.isPurchased).length;
    const totalCount = items.length;

    if (purchasedCount < totalCount) {
      const unpurchased = totalCount - purchasedCount;
      if (!confirm(`${unpurchased} item(s) are not purchased yet. Complete list anyway?`)) {
        return;
      }
    }

    setIsLoading(true);
    setError(null);

    try {
      await onCompleteList();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to complete list');
    } finally {
      setIsLoading(false);
    }
  };

  // Drag and drop handlers for item reordering
  const handleDragStart = (e: React.DragEvent<HTMLDivElement>, item: ShoppingListItem) => {
    setDraggedItem(item);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/html', e.currentTarget.outerHTML);
    
    // Make the dragged element slightly transparent
    if (e.currentTarget instanceof HTMLElement) {
      e.currentTarget.style.opacity = '0.5';
    }
  };

  const handleDragEnd = (e: React.DragEvent<HTMLDivElement>) => {
    // Reset opacity
    if (e.currentTarget instanceof HTMLElement) {
      e.currentTarget.style.opacity = '1';
    }
    setDraggedItem(null);
    setDragOverItem(null);
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault(); // Necessary to allow drop
    e.dataTransfer.dropEffect = 'move';
  };

  const handleDragEnter = (e: React.DragEvent<HTMLDivElement>, item: ShoppingListItem) => {
    e.preventDefault();
    if (draggedItem && draggedItem.id !== item.id) {
      setDragOverItem(item);
    }
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>, dropItem: ShoppingListItem) => {
    e.preventDefault();
    e.stopPropagation();

    if (!draggedItem || draggedItem.id === dropItem.id) {
      setDragOverItem(null);
      return;
    }

    // Reorder items within the same category
    const reorderedItems = [...items];
    const draggedIndex = reorderedItems.findIndex(i => i.id === draggedItem.id);
    const dropIndex = reorderedItems.findIndex(i => i.id === dropItem.id);

    if (draggedIndex === -1 || dropIndex === -1) {
      setDragOverItem(null);
      return;
    }

    // Remove dragged item and insert at new position
    const [removed] = reorderedItems.splice(draggedIndex, 1);
    reorderedItems.splice(dropIndex, 0, removed);

    // Optimistic update
    setItems(reorderedItems);
    setDragOverItem(null);

    // TODO: Call API to persist new order
    // For now, the order will reset on page reload
    console.log('Item reordered:', { from: draggedIndex, to: dropIndex });
  };

  // Group items by category
  const itemsByCategory = items.reduce((acc, item) => {
    const category = item.product?.categoryName || 'Uncategorized';
    if (!acc[category]) {
      acc[category] = [];
    }
    acc[category].push(item);
    return acc;
  }, {} as Record<string, ShoppingListItem[]>);

  // Sort items within each category by urgency
  Object.keys(itemsByCategory).forEach(category => {
    itemsByCategory[category].sort((a, b) => {
      const urgencyOrder = { 'Overdue': 0, 'DueThisWeek': 1, 'Upcoming': 2 };
      return (urgencyOrder[a.urgency] || 3) - (urgencyOrder[b.urgency] || 3);
    });
  });

  const totalItems = items.length;
  const purchasedItems = items.filter(i => i.isPurchased).length;
  const progressPercentage = totalItems > 0 ? (purchasedItems / totalItems) * 100 : 0;

  return (
    <div className="list-editor">
      <div className="list-header">
        <div className="list-info">
          <h2>{list.name}</h2>
          <p className="list-date">
            Created {new Date(list.createdDate).toLocaleDateString()}
          </p>
          {isConnected && (
            <div className="realtime-status">
              <span className="status-indicator connected"></span>
              <span className="status-text">Real-time sync active</span>
              {activeUsers.length > 0 && (
                <span className="active-users">
                  · {activeUsers.length} other {activeUsers.length === 1 ? 'user' : 'users'} viewing
                </span>
              )}
            </div>
          )}
        </div>
        {onCompleteList && list.status === 'Active' && (
          <button
            onClick={handleCompleteList}
            disabled={disabled || isLoading}
            className="btn-complete"
          >
            {isLoading ? 'Completing...' : 'Complete List'}
          </button>
        )}
      </div>

      <div className="progress-section">
        <div className="progress-bar-container">
          <div 
            className="progress-bar" 
            style={{ width: `${progressPercentage}%` }}
          />
        </div>
        <p className="progress-text">
          {purchasedItems} of {totalItems} items purchased ({Math.round(progressPercentage)}%)
        </p>
      </div>

      {error && (
        <div className="error-message">
          <span className="error-icon">⚠️</span>
          {error}
        </div>
      )}

      <div className="items-container">
        {Object.keys(itemsByCategory).sort().map(category => (
          <div key={category} className="category-section">
            <h3 className="category-title">{category}</h3>
            <div className="items-list">
              {itemsByCategory[category].map(item => (
                <div 
                  key={item.id} 
                  className={`list-item ${item.isPurchased ? 'purchased' : ''} ${
                    draggedItem?.id === item.id ? 'dragging' : ''
                  } ${dragOverItem?.id === item.id ? 'drag-over' : ''}`}
                  draggable={!disabled && !item.isPurchased}
                  onDragStart={(e) => handleDragStart(e, item)}
                  onDragEnd={handleDragEnd}
                  onDragOver={handleDragOver}
                  onDragEnter={(e) => handleDragEnter(e, item)}
                  onDrop={(e) => handleDrop(e, item)}
                >
                  <div className="drag-handle" title="Drag to reorder">
                    ⋮⋮
                  </div>
                  <div className="item-content">
                    <input
                      type="checkbox"
                      checked={item.isPurchased}
                      onChange={() => handleTogglePurchased(item)}
                      disabled={disabled}
                      className="item-checkbox"
                    />
                    
                    <div className="item-details">
                      <span className="item-name">
                        {item.product?.name || 'Unknown Product'}
                      </span>
                      {item.product?.notes && (
                        <div className="item-notes">
                          <span className="notes-icon" title="Product notes">📝</span>
                          <span className="notes-text">{item.product.notes}</span>
                        </div>
                      )}
                      <div className="item-meta">
                        <span 
                          className="urgency-badge"
                          style={{ 
                            backgroundColor: getUrgencyColor(item.urgency),
                            color: 'white'
                          }}
                        >
                          <span className="urgency-icon">
                            {getUrgencyIcon(item.urgency)}
                          </span>
                          {getUrgencyLabel(item.urgency)}
                        </span>
                        <span className="quantity-badge">
                          Qty: {item.quantity}
                        </span>
                        {item.source === 'Manual' && (
                          <span className="source-badge">Manual</span>
                        )}
                        {(() => {
                          const promo = getPromotion(item.product?.name);
                          if (promo) {
                            return (
                              <>
                                <span className="promo-badge" title={`${promo.storeName} promotion`}>
                                  🏷️ {Math.round(promo.discountPercentage)}% OFF at {promo.storeName}
                                </span>
                                <span className="promo-expiry" title="Promotion expires">
                                  ⏳ {formatExpiryDate(promo.endDate)}
                                </span>
                              </>
                            );
                          }
                          return null;
                        })()}
                      </div>
                    </div>
                  </div>

                  <button
                    onClick={() => handleRemoveItem(item)}
                    disabled={disabled}
                    className="btn-remove"
                    title="Remove item"
                  >
                    ✕
                  </button>
                </div>
              ))}
            </div>
          </div>
        ))}

        {totalItems === 0 && (
          <div className="empty-state">
            <span className="empty-icon">🛒</span>
            <p>No items in this list</p>
          </div>
        )}
      </div>
    </div>
  );
};
