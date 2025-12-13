import React, { useState, useEffect } from 'react';
import { ShoppingList, ShoppingListItem, ItemUrgency, AddItemRequest } from '../../services/api/shoppingListApi';
import priceApi, { PromotionDto } from '../../services/api/priceApi';
import './ListEditor.css';

export interface ListEditorProps {
  list: ShoppingList;
  onUpdateItem: (itemId: string, isPurchased: boolean) => Promise<void>;
  onAddItem?: (item: AddItemRequest) => Promise<void>;
  onRemoveItem: (itemId: string) => Promise<void>;
  onCompleteList?: () => Promise<void>;
  userId: string;
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
  disabled = false
}) => {
  const [items, setItems] = useState<ShoppingListItem[]>(list.items || []);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [promotions, setPromotions] = useState<Map<string, PromotionDto>>(new Map());

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
                  className={`list-item ${item.isPurchased ? 'purchased' : ''}`}
                >
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
