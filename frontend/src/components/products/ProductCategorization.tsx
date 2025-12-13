import React, { useState, useEffect } from 'react';
import './ProductCategorization.css';

export interface Category {
  id: string;
  name: string;
  description?: string;
  isCustom: boolean;
}

export interface Product {
  id: string;
  name: string;
  normalizedName: string;
  categoryId?: string;
  category?: Category;
  isManualCategory: boolean;
  averagePrice: number;
  frequency: string;
  lastPurchased?: string;
  notes?: string;
}

export interface ProductCategorizationProps {
  product: Product;
  categories: Category[];
  onCategoryChange: (productId: string, categoryId: string, isManual: boolean) => Promise<void>;
  onSuggestCategory?: (productName: string) => Promise<Category | null>;
  disabled?: boolean;
}

/**
 * Component for displaying and managing product categorization
 * Features:
 * - Display current category with visual indicator (auto vs manual)
 * - Suggest category using AI when no category assigned
 * - Dropdown for manual category selection
 * - Support for custom categories
 */
export const ProductCategorization: React.FC<ProductCategorizationProps> = ({
  product,
  categories,
  onCategoryChange,
  onSuggestCategory,
  disabled = false
}) => {
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>(
    product.categoryId || ''
  );
  const [isLoading, setIsLoading] = useState(false);
  const [isSuggesting, setIsSuggesting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [suggestedCategory, setSuggestedCategory] = useState<Category | null>(null);

  useEffect(() => {
    setSelectedCategoryId(product.categoryId || '');
  }, [product.categoryId]);

  const handleCategoryChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newCategoryId = e.target.value;
    
    if (!newCategoryId) return;

    setSelectedCategoryId(newCategoryId);
    setError(null);
    setIsLoading(true);

    try {
      await onCategoryChange(product.id, newCategoryId, true);
      setSuggestedCategory(null); // Clear suggestion after manual assignment
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update category');
      setSelectedCategoryId(product.categoryId || '');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSuggestCategory = async () => {
    if (!onSuggestCategory) return;

    setIsSuggesting(true);
    setError(null);

    try {
      const suggested = await onSuggestCategory(product.name);
      if (suggested) {
        setSuggestedCategory(suggested);
      } else {
        setError('No category suggestion available');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to suggest category');
    } finally {
      setIsSuggesting(false);
    }
  };

  const handleAcceptSuggestion = async () => {
    if (!suggestedCategory) return;

    setIsLoading(true);
    setError(null);

    try {
      await onCategoryChange(product.id, suggestedCategory.id, false);
      setSelectedCategoryId(suggestedCategory.id);
      setSuggestedCategory(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to accept suggestion');
    } finally {
      setIsLoading(false);
    }
  };

  const currentCategory = categories.find(c => c.id === selectedCategoryId);

  return (
    <div className="product-categorization">
      <div className="categorization-header">
        <label htmlFor={`category-${product.id}`} className="category-label">
          Category
        </label>
        {currentCategory && (
          <span className={`category-badge ${product.isManualCategory ? 'manual' : 'auto'}`}>
            {product.isManualCategory ? '👤 Manual' : '🤖 Auto'}
          </span>
        )}
      </div>

      <div className="categorization-controls">
        <select
          id={`category-${product.id}`}
          className="category-select"
          value={selectedCategoryId}
          onChange={handleCategoryChange}
          disabled={disabled || isLoading}
        >
          <option value="">Select category...</option>
          <optgroup label="Predefined Categories">
            {categories.filter(c => !c.isCustom).map(category => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </optgroup>
          {categories.some(c => c.isCustom) && (
            <optgroup label="Custom Categories">
              {categories.filter(c => c.isCustom).map(category => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </optgroup>
          )}
        </select>

        {!selectedCategoryId && onSuggestCategory && (
          <button
            type="button"
            className="btn btn-secondary suggest-btn"
            onClick={handleSuggestCategory}
            disabled={disabled || isSuggesting}
          >
            {isSuggesting ? '🔄 Suggesting...' : '✨ Suggest Category'}
          </button>
        )}
      </div>

      {suggestedCategory && (
        <div className="suggestion-box">
          <div className="suggestion-content">
            <span className="suggestion-icon">💡</span>
            <div className="suggestion-text">
              <strong>Suggested:</strong> {suggestedCategory.name}
              {suggestedCategory.description && (
                <p className="suggestion-description">{suggestedCategory.description}</p>
              )}
            </div>
          </div>
          <div className="suggestion-actions">
            <button
              type="button"
              className="btn btn-primary btn-sm"
              onClick={handleAcceptSuggestion}
              disabled={isLoading}
            >
              Accept
            </button>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={() => setSuggestedCategory(null)}
              disabled={isLoading}
            >
              Dismiss
            </button>
          </div>
        </div>
      )}

      {error && (
        <div className="error-message">
          <span className="error-icon">⚠️</span>
          {error}
        </div>
      )}

      {currentCategory && currentCategory.description && (
        <p className="category-description">{currentCategory.description}</p>
      )}
    </div>
  );
};
