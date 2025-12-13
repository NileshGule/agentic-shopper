import React, { useState, useEffect } from 'react';
import './ProductNotes.css';

export interface Product {
  id: string;
  name: string;
  notes?: string;
  tags?: string; // JSON string array
}

export interface ProductNotesProps {
  product: Product;
  availableTags?: string[];
  onSave: (productId: string, notes?: string, tags?: string[]) => Promise<void>;
  disabled?: boolean;
}

/**
 * Component for editing product notes and tags
 * Features:
 * - Textarea for free-form notes (max 500 characters)
 * - Tag input with autocomplete suggestions
 * - Visual tag chips with remove capability
 * - Save/cancel actions
 * - Character count for notes
 */
export const ProductNotes: React.FC<ProductNotesProps> = ({
  product,
  availableTags = [],
  onSave,
  disabled = false
}) => {
  const [notes, setNotes] = useState<string>(product.notes || '');
  const [tags, setTags] = useState<string[]>([]);
  const [tagInput, setTagInput] = useState<string>('');
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filteredSuggestions, setFilteredSuggestions] = useState<string[]>([]);
  const [showSuggestions, setShowSuggestions] = useState(false);

  const MAX_NOTES_LENGTH = 500;

  // Parse tags from JSON string
  useEffect(() => {
    if (product.tags) {
      try {
        const parsedTags = JSON.parse(product.tags);
        setTags(Array.isArray(parsedTags) ? parsedTags : []);
      } catch {
        setTags([]);
      }
    } else {
      setTags([]);
    }
    setNotes(product.notes || '');
  }, [product.notes, product.tags]);

  // Filter suggestions based on input
  useEffect(() => {
    if (tagInput.trim()) {
      const filtered = availableTags.filter(
        tag =>
          tag.toLowerCase().includes(tagInput.toLowerCase()) &&
          !tags.includes(tag)
      );
      setFilteredSuggestions(filtered);
      setShowSuggestions(filtered.length > 0);
    } else {
      setFilteredSuggestions([]);
      setShowSuggestions(false);
    }
  }, [tagInput, tags, availableTags]);

  const handleNotesChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const value = e.target.value;
    if (value.length <= MAX_NOTES_LENGTH) {
      setNotes(value);
    }
  };

  const handleTagInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setTagInput(e.target.value);
  };

  const handleTagInputKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && tagInput.trim()) {
      e.preventDefault();
      addTag(tagInput.trim());
    } else if (e.key === 'Backspace' && !tagInput && tags.length > 0) {
      // Remove last tag on backspace when input is empty
      removeTag(tags[tags.length - 1]);
    } else if (e.key === 'Escape') {
      setShowSuggestions(false);
    }
  };

  const addTag = (tag: string) => {
    const normalizedTag = tag.toLowerCase();
    if (!tags.some(t => t.toLowerCase() === normalizedTag)) {
      setTags([...tags, tag]);
      setTagInput('');
      setShowSuggestions(false);
    }
  };

  const removeTag = (tagToRemove: string) => {
    setTags(tags.filter(tag => tag !== tagToRemove));
  };

  const handleSuggestionClick = (suggestion: string) => {
    addTag(suggestion);
  };

  const handleSave = async () => {
    setError(null);
    setIsSaving(true);

    try {
      await onSave(
        product.id,
        notes.trim() || undefined,
        tags.length > 0 ? tags : undefined
      );
      setIsEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save notes/tags');
    } finally {
      setIsSaving(false);
    }
  };

  const handleCancel = () => {
    setNotes(product.notes || '');
    if (product.tags) {
      try {
        const parsedTags = JSON.parse(product.tags);
        setTags(Array.isArray(parsedTags) ? parsedTags : []);
      } catch {
        setTags([]);
      }
    } else {
      setTags([]);
    }
    setTagInput('');
    setError(null);
    setIsEditing(false);
  };

  const hasChanges = () => {
    const currentTags = product.tags ? JSON.parse(product.tags) : [];
    return (
      notes !== (product.notes || '') ||
      JSON.stringify(tags) !== JSON.stringify(currentTags)
    );
  };

  return (
    <div className="product-notes">
      <div className="product-notes-header">
        <h3>Notes & Tags</h3>
        {!isEditing && !disabled && (
          <button
            className="btn-edit"
            onClick={() => setIsEditing(true)}
            aria-label="Edit notes and tags"
          >
            Edit
          </button>
        )}
      </div>

      {error && (
        <div className="product-notes-error" role="alert">
          {error}
        </div>
      )}

      <div className="product-notes-content">
        {/* Notes Section */}
        <div className="notes-section">
          <label htmlFor={`notes-${product.id}`} className="notes-label">
            Notes
          </label>
          {isEditing ? (
            <>
              <textarea
                id={`notes-${product.id}`}
                className="notes-textarea"
                value={notes}
                onChange={handleNotesChange}
                placeholder="Add notes about this product (e.g., preferred brand, dietary info, storage tips)"
                disabled={disabled || isSaving}
                rows={4}
              />
              <div className="notes-char-count">
                {notes.length}/{MAX_NOTES_LENGTH} characters
              </div>
            </>
          ) : (
            <div className="notes-display">
              {product.notes || <em className="notes-empty">No notes</em>}
            </div>
          )}
        </div>

        {/* Tags Section */}
        <div className="tags-section">
          <label htmlFor={`tags-${product.id}`} className="tags-label">
            Tags
          </label>
          
          {isEditing ? (
            <div className="tags-input-container">
              <div className="tags-chips">
                {tags.map((tag, index) => (
                  <span key={index} className="tag-chip">
                    {tag}
                    <button
                      className="tag-remove"
                      onClick={() => removeTag(tag)}
                      disabled={disabled || isSaving}
                      aria-label={`Remove tag ${tag}`}
                    >
                      ×
                    </button>
                  </span>
                ))}
                <input
                  id={`tags-${product.id}`}
                  type="text"
                  className="tags-input"
                  value={tagInput}
                  onChange={handleTagInputChange}
                  onKeyDown={handleTagInputKeyDown}
                  onFocus={() => tagInput && setShowSuggestions(true)}
                  onBlur={() => setTimeout(() => setShowSuggestions(false), 200)}
                  placeholder={tags.length === 0 ? "Add tags (e.g., organic, gluten-free)" : ""}
                  disabled={disabled || isSaving}
                />
              </div>
              
              {showSuggestions && filteredSuggestions.length > 0 && (
                <ul className="tags-suggestions" role="listbox">
                  {filteredSuggestions.slice(0, 5).map((suggestion, index) => (
                    <li
                      key={index}
                      className="tags-suggestion-item"
                      onClick={() => handleSuggestionClick(suggestion)}
                      role="option"
                    >
                      {suggestion}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          ) : (
            <div className="tags-display">
              {tags.length > 0 ? (
                <div className="tags-chips-readonly">
                  {tags.map((tag, index) => (
                    <span key={index} className="tag-chip-readonly">
                      {tag}
                    </span>
                  ))}
                </div>
              ) : (
                <em className="tags-empty">No tags</em>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Action Buttons */}
      {isEditing && (
        <div className="product-notes-actions">
          <button
            className="btn-cancel"
            onClick={handleCancel}
            disabled={isSaving}
          >
            Cancel
          </button>
          <button
            className="btn-save"
            onClick={handleSave}
            disabled={!hasChanges() || isSaving}
          >
            {isSaving ? 'Saving...' : 'Save'}
          </button>
        </div>
      )}
    </div>
  );
};

export default ProductNotes;
