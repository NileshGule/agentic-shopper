import React, { useState } from 'react';
import './ListCreationModal.css';

export interface ListCreationModalProps {
  onCreateList: (name: string) => Promise<void>;
  onCancel: () => void;
}

/**
 * Modal dialog for creating new shopping lists
 * Features:
 * - Name input with validation
 * - Create/Cancel actions
 * - Error handling
 */
export const ListCreationModal: React.FC<ListCreationModalProps> = ({
  onCreateList,
  onCancel
}) => {
  const [listName, setListName] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const trimmedName = listName.trim();
    
    if (!trimmedName) {
      setError('Please enter a list name');
      return;
    }

    if (trimmedName.length > 100) {
      setError('List name must be 100 characters or less');
      return;
    }

    setIsCreating(true);
    setError(null);

    try {
      await onCreateList(trimmedName);
      // onCreateList callback should close the modal
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create list');
      setIsCreating(false);
    }
  };

  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (e.target === e.currentTarget) {
      onCancel();
    }
  };

  return (
    <div className="modal-backdrop" onClick={handleBackdropClick}>
      <div className="modal-content">
        <div className="modal-header">
          <h2>Create New Shopping List</h2>
          <button 
            onClick={onCancel} 
            className="modal-close"
            disabled={isCreating}
            aria-label="Close"
          >
            ✕
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && (
              <div className="error-message">
                <span className="error-icon">⚠️</span>
                {error}
              </div>
            )}

            <div className="form-group">
              <label htmlFor="list-name">List Name</label>
              <input
                id="list-name"
                type="text"
                value={listName}
                onChange={(e) => setListName(e.target.value)}
                placeholder="e.g., Weekly Shopping, Party Supplies"
                maxLength={100}
                autoFocus
                disabled={isCreating}
                className="form-input"
              />
              <p className="form-hint">
                {listName.length}/100 characters
              </p>
            </div>
          </div>

          <div className="modal-footer">
            <button
              type="button"
              onClick={onCancel}
              disabled={isCreating}
              className="btn-secondary"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isCreating || !listName.trim()}
              className="btn-primary"
            >
              {isCreating ? 'Creating...' : 'Create List'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
