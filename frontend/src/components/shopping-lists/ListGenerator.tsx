import React, { useState } from 'react';
import { GenerateListRequest, GeneratedListResponse, ItemUrgency } from '../../services/api/shoppingListApi';
import './ListGenerator.css';

export interface ListGeneratorProps {
  familyId: string;
  userId: string;
  onListGenerated: (list: GeneratedListResponse) => void;
  onCancel?: () => void;
  disabled?: boolean;
}

/**
 * Component for generating shopping lists based on purchase frequency analysis
 * Features:
 * - Configure list generation settings (urgency levels)
 * - Category filtering
 * - Custom list naming
 * - Preview suggested items before generation
 */
export const ListGenerator: React.FC<ListGeneratorProps> = ({
  familyId,
  userId,
  onListGenerated,
  onCancel,
  disabled = false
}) => {
  const [listName, setListName] = useState(`Shopping List - ${new Date().toLocaleDateString()}`);
  const [includeDueItems, setIncludeDueItems] = useState(true);
  const [includeOverdueItems, setIncludeOverdueItems] = useState(true);
  const [includeUpcomingItems, setIncludeUpcomingItems] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleGenerate = async () => {
    setError(null);
    setIsGenerating(true);

    try {
      const request: GenerateListRequest = {
        familyId,
        createdBy: userId,
        listName,
        includeDueItems,
        includeOverdueItems,
        includeUpcomingItems,
        categoryFilter: []
      };

      // API call would go here
      const response = await import('../../services/api/shoppingListApi').then(m => 
        m.shoppingListApi.generateList(request)
      );

      onListGenerated(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate shopping list');
    } finally {
      setIsGenerating(false);
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

  return (
    <div className="list-generator">
      <h2>Generate Shopping List</h2>

      <div className="form-section">
        <label htmlFor="list-name">List Name</label>
        <input
          id="list-name"
          type="text"
          value={listName}
          onChange={(e) => setListName(e.target.value)}
          disabled={disabled || isGenerating}
          placeholder="Enter list name"
          className="list-name-input"
        />
      </div>

      <div className="urgency-settings">
        <h3>Item Urgency Filters</h3>
        <p className="description">Select which items to include based on urgency</p>

        <div className="urgency-option">
          <label>
            <input
              type="checkbox"
              checked={includeOverdueItems}
              onChange={(e) => setIncludeOverdueItems(e.target.checked)}
              disabled={disabled || isGenerating}
            />
            <span className="urgency-label">
              <span className="urgency-icon" style={{ color: getUrgencyColor('Overdue') }}>
                {getUrgencyIcon('Overdue')}
              </span>
              <span className="urgency-text">
                <strong>Overdue</strong>
                <span className="urgency-description">
                  Items that should have been purchased already
                </span>
              </span>
            </span>
          </label>
        </div>

        <div className="urgency-option">
          <label>
            <input
              type="checkbox"
              checked={includeDueItems}
              onChange={(e) => setIncludeDueItems(e.target.checked)}
              disabled={disabled || isGenerating}
            />
            <span className="urgency-label">
              <span className="urgency-icon" style={{ color: getUrgencyColor('DueThisWeek') }}>
                {getUrgencyIcon('DueThisWeek')}
              </span>
              <span className="urgency-text">
                <strong>Due This Week</strong>
                <span className="urgency-description">
                  Items that need to be purchased soon
                </span>
              </span>
            </span>
          </label>
        </div>

        <div className="urgency-option">
          <label>
            <input
              type="checkbox"
              checked={includeUpcomingItems}
              onChange={(e) => setIncludeUpcomingItems(e.target.checked)}
              disabled={disabled || isGenerating}
            />
            <span className="urgency-label">
              <span className="urgency-icon" style={{ color: getUrgencyColor('Upcoming') }}>
                {getUrgencyIcon('Upcoming')}
              </span>
              <span className="urgency-text">
                <strong>Upcoming</strong>
                <span className="urgency-description">
                  Items that will be needed in the future
                </span>
              </span>
            </span>
          </label>
        </div>
      </div>

      {error && (
        <div className="error-message">
          <span className="error-icon">⚠️</span>
          {error}
        </div>
      )}

      <div className="action-buttons">
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            disabled={disabled || isGenerating}
            className="btn-secondary"
          >
            Cancel
          </button>
        )}
        <button
          type="button"
          onClick={handleGenerate}
          disabled={disabled || isGenerating || (!includeDueItems && !includeOverdueItems && !includeUpcomingItems)}
          className="btn-primary"
        >
          {isGenerating ? (
            <>
              <span className="spinner"></span>
              Generating...
            </>
          ) : (
            <>
              <span className="icon">🛒</span>
              Generate List
            </>
          )}
        </button>
      </div>

      {!includeDueItems && !includeOverdueItems && !includeUpcomingItems && (
        <div className="warning-message">
          <span className="warning-icon">ℹ️</span>
          Please select at least one urgency level to generate a list
        </div>
      )}
    </div>
  );
};
