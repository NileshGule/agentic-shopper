import { useState, useEffect } from 'react';
import { ProductCategorization } from '../components/products/ProductCategorization';
import { FrequencyAssignment } from '../components/products/FrequencyAssignment';
import { ProductNotes } from '../components/products/ProductNotes';
import Button from '../components/common/Button';
import SkeletonLoader from '../components/common/SkeletonLoader';
import { productApi, PurchaseFrequency } from '../services/api/productApi';
import { categorizationApi } from '../services/api/categorizationApi';
import { frequencyApi } from '../services/api/frequencyApi';
import { COMMON_PRODUCT_TAGS } from '../constants/productTags';
import type { 
  Product
} from '../services/api/productApi';
import type { 
  Category, 
  AssignCategoryRequest 
} from '../services/api/categorizationApi';
import type { 
  FrequencyOverrideRequest 
} from '../services/api/frequencyApi';
import './ProductsPage.css';

type FilterMode = 'all' | 'uncategorized' | 'category' | 'frequency' | 'tag' | 'notes';

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [availableTags, setAvailableTags] = useState<string[]>([...COMMON_PRODUCT_TAGS]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterMode, setFilterMode] = useState<FilterMode>('all');
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>('');
  const [selectedFrequency, setSelectedFrequency] = useState<PurchaseFrequency | ''>('');
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [notesSearchTerm, setNotesSearchTerm] = useState('');
  const [refreshTrigger, setRefreshTrigger] = useState(0);

  // Fetch categories on mount
  useEffect(() => {
    const fetchCategories = async () => {
      try {
        const cats = await categorizationApi.getCategories();
        setCategories(cats);
      } catch (err) {
        console.error('Failed to fetch categories:', err);
      }
    };
    fetchCategories();
  }, []);

  // Fetch all tags to enhance autocomplete
  useEffect(() => {
    const fetchTags = async () => {
      try {
        const tags = await productApi.getAllTags();
        // Combine common tags with user-created tags, removing duplicates
        const allTags = Array.from(new Set([...COMMON_PRODUCT_TAGS, ...tags]));
        setAvailableTags(allTags);
      } catch (err) {
        console.error('Failed to fetch tags:', err);
        // Fallback to common tags only
        setAvailableTags([...COMMON_PRODUCT_TAGS]);
      }
    };
    fetchTags();
  }, [refreshTrigger]);

  // Fetch products based on filters
  useEffect(() => {
    const fetchProducts = async () => {
      setLoading(true);
      setError(null);

      try {
        let result: Product[] = [];

        if (filterMode === 'uncategorized') {
          result = await productApi.getUncategorizedProducts();
        } else if (filterMode === 'category' && selectedCategoryId) {
          result = await productApi.getProductsByCategory(selectedCategoryId);
        } else if (filterMode === 'frequency' && selectedFrequency) {
          result = await productApi.getProductsByFrequency(selectedFrequency as PurchaseFrequency);
        } else if (filterMode === 'tag' && selectedTags.length > 0) {
          result = await productApi.filterByTags(selectedTags);
        } else if (filterMode === 'notes' && notesSearchTerm.trim()) {
          result = await productApi.searchByNotes(notesSearchTerm.trim());
        } else if (searchTerm.trim()) {
          result = await productApi.searchProducts(searchTerm.trim());
        } else {
          const response = await productApi.getAllProducts();
          result = response.products;
        }

        setProducts(result);
      } catch (err) {
        console.error('Failed to fetch products:', err);
        setError('Failed to load products. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    fetchProducts();
  }, [filterMode, selectedCategoryId, selectedFrequency, selectedTags, notesSearchTerm, searchTerm, refreshTrigger]);

  const handleCategoryChange = async (productId: string, categoryId: string, _isManual: boolean) => {
    try {
      const request: AssignCategoryRequest = {
        productId,
        categoryId,
      };
      await categorizationApi.assignCategory(request);
      
      // Refresh products to show updated category
      setRefreshTrigger((prev) => prev + 1);
    } catch (err) {
      console.error('Failed to update category:', err);
      throw new Error('Failed to update category. Please try again.');
    }
  };

  const handleSuggestCategory = async (productName: string): Promise<Category | null> => {
    try {
      const response = await categorizationApi.suggestCategory({ productName });
      
      if (response.categoryId) {
        const category = categories.find((c) => c.id === response.categoryId);
        return category || null;
      }
      
      return null;
    } catch (err) {
      console.error('Failed to suggest category:', err);
      return null;
    }
  };

  const handleFrequencyChange = async (
    productId: string, 
    frequency: PurchaseFrequency, 
    _isManual: boolean
  ) => {
    try {
      const request: FrequencyOverrideRequest = {
        productId,
        frequency: frequency as any, // Convert enum to string type
      };
      await frequencyApi.overrideFrequency(request);
      
      // Refresh products to show updated frequency
      setRefreshTrigger((prev) => prev + 1);
    } catch (err) {
      console.error('Failed to update frequency:', err);
      throw new Error('Failed to update frequency. Please try again.');
    }
  };

  const handleTogglePause = async (productId: string, isPaused: boolean) => {
    try {
      if (isPaused) {
        await frequencyApi.pauseFrequency(productId);
      } else {
        await frequencyApi.resumeFrequency(productId);
      }
      
      // Refresh products to show updated pause state
      setRefreshTrigger((prev) => prev + 1);
    } catch (err) {
      console.error('Failed to toggle pause:', err);
      throw new Error('Failed to toggle pause. Please try again.');
    }
  };

  const handleRecalculate = async (productId: string) => {
    try {
      await frequencyApi.calculateFrequency(productId, true);
      
      // Refresh products to show recalculated frequency
      setRefreshTrigger((prev) => prev + 1);
    } catch (err) {
      console.error('Failed to recalculate frequency:', err);
      throw new Error('Failed to recalculate frequency. Please try again.');
    }
  };

  const handleMarkPurchased = async (productId: string, purchaseDate: Date) => {
    try {
      await frequencyApi.markAsPurchasedExternally(productId, purchaseDate);
      
      // Refresh products to show updated frequency
      setRefreshTrigger((prev) => prev + 1);
    } catch (err) {
      console.error('Failed to mark as purchased:', err);
      throw new Error('Failed to mark as purchased. Please try again.');
    }
  };

  const handleSaveNotesAndTags = async (
    productId: string, 
    notes?: string, 
    tags?: string[]
  ) => {
    try {
      await productApi.updateNotesAndTags(productId, notes, tags);
      
      // Refresh products to show updated notes/tags and update tag list
      setRefreshTrigger((prev) => prev + 1);
    } catch (err) {
      console.error('Failed to save notes and tags:', err);
      throw new Error('Failed to save notes and tags. Please try again.');
    }
  };

  const handleClearFilters = () => {
    setSearchTerm('');
    setFilterMode('all');
    setSelectedCategoryId('');
    setSelectedFrequency('');
    setSelectedTags([]);
    setNotesSearchTerm('');
  };

  const handleToggleTag = (tag: string) => {
    setSelectedTags((prev) =>
      prev.includes(tag) ? prev.filter((t) => t !== tag) : [...prev, tag]
    );
  };

  return (
    <div className="products-page">
      <div className="page-header">
        <h1>📦 Product Management</h1>
        <p className="page-description">
          View, categorize, and manage purchase frequency for your products
        </p>
      </div>

      {/* Filters Section */}
      <div className="filters-section">
        <div className="search-bar">
          <input
            type="text"
            className="search-input"
            placeholder="🔍 Search products by name..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>

        <div className="filter-controls">
          <div className="filter-group">
            <label className="filter-label">Filter By:</label>
            <select
              className="filter-select"
              value={filterMode}
              onChange={(e) => setFilterMode(e.target.value as FilterMode)}
            >
              <option value="all">All Products</option>
              <option value="uncategorized">Uncategorized Only</option>
              <option value="category">By Category</option>
              <option value="frequency">By Frequency</option>
              <option value="tag">By Tags</option>
              <option value="notes">By Notes Content</option>
            </select>
          </div>

          {filterMode === 'category' && (
            <div className="filter-group">
              <label className="filter-label">Category:</label>
              <select
                className="filter-select"
                value={selectedCategoryId}
                onChange={(e) => setSelectedCategoryId(e.target.value)}
              >
                <option value="">Select a category...</option>
                {categories.map((cat) => (
                  <option key={cat.id} value={cat.id}>
                    {cat.name} {cat.isCustom ? '(Custom)' : ''}
                  </option>
                ))}
              </select>
            </div>
          )}

          {filterMode === 'frequency' && (
            <div className="filter-group">
              <label className="filter-label">Frequency:</label>
              <select
                className="filter-select"
                value={selectedFrequency}
                onChange={(e) => setSelectedFrequency(e.target.value as PurchaseFrequency)}
              >
                <option value="">Select frequency...</option>
                <option value={PurchaseFrequency.Weekly}>Weekly</option>
                <option value={PurchaseFrequency.Fortnightly}>Fortnightly</option>
                <option value={PurchaseFrequency.Monthly}>Monthly</option>
                <option value={PurchaseFrequency.Quarterly}>Quarterly</option>
                <option value={PurchaseFrequency.Annually}>Annually</option>
                <option value={PurchaseFrequency.Occasional}>Occasional</option>
                <option value={PurchaseFrequency.Unknown}>Unknown</option>
              </select>
            </div>
          )}

          {filterMode === 'tag' && (
            <div className="filter-group tag-filter-group">
              <label className="filter-label">Select Tags:</label>
              <div className="tag-chips-filter">
                {availableTags.map((tag) => (
                  <button
                    key={tag}
                    className={`tag-chip-filter ${selectedTags.includes(tag) ? 'selected' : ''}`}
                    onClick={() => handleToggleTag(tag)}
                  >
                    {tag}
                    {selectedTags.includes(tag) && ' ✓'}
                  </button>
                ))}
              </div>
              {selectedTags.length > 0 && (
                <div className="selected-tags-info">
                  {selectedTags.length} tag{selectedTags.length !== 1 ? 's' : ''} selected
                </div>
              )}
            </div>
          )}

          {filterMode === 'notes' && (
            <div className="filter-group">
              <label className="filter-label">Search Notes:</label>
              <input
                type="text"
                className="filter-input"
                placeholder="Enter search term..."
                value={notesSearchTerm}
                onChange={(e) => setNotesSearchTerm(e.target.value)}
              />
            </div>
          )}

          {(searchTerm || filterMode !== 'all') && (
            <Button variant="secondary" onClick={handleClearFilters}>
              Clear Filters
            </Button>
          )}
        </div>
      </div>

      {/* Products List */}
      <div className="products-content">
        {loading && (
          <div className="products-list">
            <div className="products-count" style={{ opacity: 0.5 }}>Loading products...</div>
            {Array.from({ length: 6 }).map((_, index) => (
              <div key={index} className="product-card" style={{ padding: '20px' }}>
                <SkeletonLoader variant="text" width="60%" height={24} />
                <SkeletonLoader variant="text" width="40%" height={16} />
                <SkeletonLoader variant="rectangular" width="100%" height={100} />
                <div style={{ display: 'flex', gap: '8px', marginTop: '12px' }}>
                  <SkeletonLoader variant="text" width="30%" height={14} />
                  <SkeletonLoader variant="text" width="25%" height={14} />
                </div>
              </div>
            ))}
          </div>
        )}
        
        {!loading && error && <div className="error-message">{error}</div>}
        
        {!loading && !error && products.length === 0 && (
          <div className="empty-state">
            <p>No products found.</p>
            {searchTerm && <p>Try adjusting your search or filters.</p>}
          </div>
        )}

        {!loading && !error && products.length > 0 && (
          <div className="products-list">
            <div className="products-count">
              Showing {products.length} product{products.length !== 1 ? 's' : ''}
            </div>

            {products.map((product) => (
              <div key={product.id} className="product-card">
                <div className="product-header">
                  <h3 className="product-name">{product.name}</h3>
                  <div className="product-meta">
                    <span className="average-price">
                      Avg: ${product.averagePrice.toFixed(2)}
                    </span>
                    {product.lastPurchased && (
                      <span className="last-purchased">
                        Last: {new Date(product.lastPurchased).toLocaleDateString()}
                      </span>
                    )}
                  </div>
                </div>

                <div className="product-body">
                  <div className="product-section">
                    <h4 className="section-title">Category</h4>
                    <ProductCategorization
                      product={product}
                      categories={categories}
                      onCategoryChange={handleCategoryChange}
                      onSuggestCategory={handleSuggestCategory}
                    />
                  </div>

                  <div className="product-section">
                    <h4 className="section-title">Purchase Frequency</h4>
                    <FrequencyAssignment
                      productId={product.id}
                      frequencyData={{
                        frequency: product.frequency,
                        isManual: false, // TODO: Get from backend
                        purchaseCount: 0, // TODO: Get from backend
                        lastPurchased: product.lastPurchased,
                        averageDaysBetween: 0, // TODO: Calculate from backend
                        nextExpectedDate: undefined, // TODO: Calculate from backend
                        isPaused: product.frequencyPaused,
                      }}
                      onFrequencyChange={handleFrequencyChange}
                      onTogglePause={handleTogglePause}
                      onRecalculate={handleRecalculate}
                      onMarkPurchased={handleMarkPurchased}
                    />
                  </div>

                  <div className="product-section">
                    <h4 className="section-title">Notes & Tags</h4>
                    <ProductNotes
                      product={product}
                      availableTags={availableTags}
                      onSave={handleSaveNotesAndTags}
                    />
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
