import React, { useState, useEffect } from 'react';
import './ListSharing.css';

export interface User {
  id: string;
  name: string;
  email: string;
}

export interface ListSharingProps {
  listId: string;
  sharedWith: string[]; // Array of user IDs
  familyMembers: User[]; // Available family members to share with
  onShare: (userIds: string[]) => Promise<void>;
}

/**
 * Component for managing shopping list sharing with family members
 * Features:
 * - View currently shared users
 * - Add/remove family members from shared list
 * - Visual indication of sharing status
 */
export const ListSharing: React.FC<ListSharingProps> = ({
  listId,
  sharedWith,
  familyMembers,
  onShare
}) => {
  const [selectedUsers, setSelectedUsers] = useState<string[]>(sharedWith);
  const [isUpdating, setIsUpdating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasChanges, setHasChanges] = useState(false);

  useEffect(() => {
    setSelectedUsers(sharedWith);
  }, [sharedWith]);

  useEffect(() => {
    const changed = JSON.stringify([...selectedUsers].sort()) !== 
                    JSON.stringify([...sharedWith].sort());
    setHasChanges(changed);
  }, [selectedUsers, sharedWith]);

  const handleToggleUser = (userId: string) => {
    if (selectedUsers.includes(userId)) {
      setSelectedUsers(selectedUsers.filter(id => id !== userId));
    } else {
      setSelectedUsers([...selectedUsers, userId]);
    }
  };

  const handleSaveChanges = async () => {
    setIsUpdating(true);
    setError(null);

    try {
      await onShare(selectedUsers);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update sharing');
      // Revert on error
      setSelectedUsers(sharedWith);
    } finally {
      setIsUpdating(false);
    }
  };

  const handleCancel = () => {
    setSelectedUsers(sharedWith);
    setError(null);
  };

  const getSharedUserCount = () => {
    return selectedUsers.length;
  };

  return (
    <div className="list-sharing">
      <div className="sharing-header">
        <h3>Share List</h3>
        <p className="sharing-subtitle">
          {getSharedUserCount() === 0 
            ? 'Not shared with anyone'
            : `Shared with ${getSharedUserCount()} ${getSharedUserCount() === 1 ? 'person' : 'people'}`
          }
        </p>
      </div>

      {error && (
        <div className="error-message">
          <span className="error-icon">⚠️</span>
          {error}
        </div>
      )}

      <div className="family-members-list">
        {familyMembers.length === 0 ? (
          <div className="empty-state">
            <p>No other family members to share with</p>
          </div>
        ) : (
          familyMembers.map(member => {
            const isSelected = selectedUsers.includes(member.id);
            return (
              <div 
                key={member.id} 
                className={`member-item ${isSelected ? 'selected' : ''}`}
              >
                <div className="member-info">
                  <div className="member-avatar">
                    {member.name.charAt(0).toUpperCase()}
                  </div>
                  <div className="member-details">
                    <div className="member-name">{member.name}</div>
                    <div className="member-email">{member.email}</div>
                  </div>
                </div>
                <label className="checkbox-container">
                  <input
                    type="checkbox"
                    checked={isSelected}
                    onChange={() => handleToggleUser(member.id)}
                    disabled={isUpdating}
                  />
                  <span className="checkmark"></span>
                </label>
              </div>
            );
          })
        )}
      </div>

      {hasChanges && (
        <div className="sharing-actions">
          <button
            onClick={handleCancel}
            disabled={isUpdating}
            className="btn-cancel"
          >
            Cancel
          </button>
          <button
            onClick={handleSaveChanges}
            disabled={isUpdating}
            className="btn-save"
          >
            {isUpdating ? 'Saving...' : 'Save Changes'}
          </button>
        </div>
      )}

      {selectedUsers.length > 0 && (
        <div className="sharing-info">
          <span className="info-icon">ℹ️</span>
          <p>
            Shared members can view and edit this list. Changes sync in real-time.
          </p>
        </div>
      )}
    </div>
  );
};
