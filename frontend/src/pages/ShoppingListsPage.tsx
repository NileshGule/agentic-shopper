import React, { useState, useEffect } from 'react';
import { ListGenerator } from '../components/shopping-lists/ListGenerator';
import { ListEditor } from '../components/shopping-lists/ListEditor';
import { ListCreationModal } from '../components/shopping-lists/ListCreationModal';
import { 
  shoppingListApi, 
  ShoppingList, 
  GeneratedListResponse,
  AddItemRequest,
  ItemUrgency 
} from '../services/api/shoppingListApi';
import './ShoppingListsPage.css';

export default function ShoppingListsPage() {
  const [lists, setLists] = useState<ShoppingList[]>([]);
  const [selectedList, setSelectedList] = useState<ShoppingList | null>(null);
  const [showGenerator, setShowGenerator] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // TODO: Replace with actual user/family data from auth context
  const familyId = 'demo-family-id';
  const userId = 'demo-user-id';

  useEffect(() => {
    loadLists();
  }, []);

  const loadLists = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const activeLists = await shoppingListApi.getActiveListsByFamily(familyId);
      setLists(activeLists);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load shopping lists');
    } finally {
      setIsLoading(false);
    }
  };

  const handleListGenerated = async (generatedList: GeneratedListResponse) => {
    try {
      // Reload lists to include the newly generated one
      await loadLists();
      setShowGenerator(false);

      // Select the newly generated list
      const newList = await shoppingListApi.getListById(generatedList.listId);
      setSelectedList(newList);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load generated list');
    }
  };

  const handleUpdateItem = async (itemId: string, isPurchased: boolean) => {
    await shoppingListApi.updateItemPurchaseStatus(itemId, isPurchased);
    
    // Refresh selected list
    if (selectedList) {
      const updatedList = await shoppingListApi.getListById(selectedList.id);
      setSelectedList(updatedList);
    }
  };

  const handleAddItem = async (item: AddItemRequest) => {
    if (!selectedList) return;

    await shoppingListApi.addItemToList(selectedList.id, item);

    // Refresh selected list
    const updatedList = await shoppingListApi.getListById(selectedList.id);
    setSelectedList(updatedList);
  };

  const handleRemoveItem = async (itemId: string) => {
    await shoppingListApi.removeItemFromList(itemId);

    // Refresh selected list
    if (selectedList) {
      const updatedList = await shoppingListApi.getListById(selectedList.id);
      setSelectedList(updatedList);
    }
  };

  const handleCompleteList = async () => {
    if (!selectedList) return;

    await shoppingListApi.completeList(selectedList.id);

    // Reload lists and clear selection
    await loadLists();
    setSelectedList(null);
  };

  const handleSelectList = async (listId: string) => {
    try {
      const list = await shoppingListApi.getListById(listId);
      setSelectedList(list);
      setShowGenerator(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load list');
    }
  };

  const handleCreateList = async (name: string) => {
    try {
      const newList = await shoppingListApi.createList(familyId, name, userId);
      await loadLists();
      setSelectedList(newList);
      setShowCreateModal(false);
    } catch (err) {
      throw new Error(err instanceof Error ? err.message : 'Failed to create list');
    }
  };

  const handleArchiveList = async (listId: string) => {
    if (!confirm('Archive this list? You can restore it later.')) {
      return;
    }

    try {
      await shoppingListApi.archiveList(listId);
      await loadLists();
      
      // Clear selection if archived list was selected
      if (selectedList?.id === listId) {
        setSelectedList(null);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to archive list');
    }
  };

  const handleCopyList = async (listId: string) => {
    const originalList = lists.find(l => l.id === listId);
    const newName = prompt(`Copy list "${originalList?.name}".\nEnter new list name:`);
    
    if (!newName || !newName.trim()) {
      return;
    }

    try {
      const copiedList = await shoppingListApi.copyList(listId, newName.trim(), userId);
      await loadLists();
      setSelectedList(copiedList);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to copy list');
    }
  };

  return (
    <div className="shopping-lists-page">
      <div className="page-header">
        <h1>Shopping Lists</h1>
        <div className="header-actions">
          <button 
            onClick={() => setShowCreateModal(true)}
            className="btn-secondary"
            title="Create empty list"
          >
            <span className="icon">📝</span>
            New Empty List
          </button>
          <button 
            onClick={() => {
              setShowGenerator(true);
              setSelectedList(null);
            }}
            className="btn-primary"
          >
            <span className="icon">🛒</span>
            Generate From Patterns
          </button>
        </div>
      </div>

      {error && (
        <div className="error-banner">
          <span className="error-icon">⚠️</span>
          {error}
          <button 
            onClick={() => setError(null)} 
            className="error-close"
          >
            ✕
          </button>
        </div>
      )}

      <div className="content-layout">
        <aside className="lists-sidebar">
          <h2>Your Lists</h2>
          {isLoading ? (
            <div className="loading">Loading lists...</div>
          ) : lists.length === 0 ? (
            <div className="empty-lists">
              <p>No active lists</p>
              <p className="hint">Create or generate a list to get started!</p>
            </div>
          ) : (
            <div className="lists-list">
              {lists.map(list => (
                <div 
                  key={list.id}
                  className={`list-item ${selectedList?.id === list.id ? 'active' : ''}`}
                >
                  <div 
                    className="list-item-content"
                    onClick={() => handleSelectList(list.id)}
                  >
                    <h3>{list.name}</h3>
                    <p className="list-date">
                      {new Date(list.createdDate).toLocaleDateString()}
                    </p>
                    <p className="list-stats">
                      {list.items?.length || 0} items
                    </p>
                  </div>
                  <div className="list-item-actions">
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        handleCopyList(list.id);
                      }}
                      className="icon-button"
                      title="Copy list"
                    >
                      📋
                    </button>
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        handleArchiveList(list.id);
                      }}
                      className="icon-button"
                      title="Archive list"
                    >
                      📦
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </aside>

        <main className="main-content">
          {showGenerator ? (
            <ListGenerator
              familyId={familyId}
              userId={userId}
              onListGenerated={handleListGenerated}
              onCancel={() => setShowGenerator(false)}
            />
          ) : selectedList ? (
            <ListEditor
              list={selectedList}
              onUpdateItem={handleUpdateItem}
              onAddItem={handleAddItem}
              onRemoveItem={handleRemoveItem}
              onCompleteList={handleCompleteList}
              userId={userId}
            />
          ) : (
            <div className="welcome-message">
              <span className="welcome-icon">🛒</span>
              <h2>Welcome to Shopping Lists</h2>
              <p>Select a list from the sidebar or create a new one</p>
            </div>
          )}
        </main>
      </div>

      {showCreateModal && (
        <ListCreationModal
          onCreateList={handleCreateList}
          onCancel={() => setShowCreateModal(false)}
        />
      )}
    </div>
  );
}
