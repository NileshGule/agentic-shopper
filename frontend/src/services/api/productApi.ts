import api from './client';

// Type union matching backend enum
export type PurchaseFrequency =
  | 'Unknown'
  | 'Weekly'
  | 'Fortnightly'
  | 'Monthly'
  | 'Quarterly'
  | 'Annually'
  | 'Occasional';

// Enum-like object for convenient usage
export const PurchaseFrequency = {
  Unknown: 'Unknown' as PurchaseFrequency,
  Weekly: 'Weekly' as PurchaseFrequency,
  Fortnightly: 'Fortnightly' as PurchaseFrequency,
  Monthly: 'Monthly' as PurchaseFrequency,
  Quarterly: 'Quarterly' as PurchaseFrequency,
  Annually: 'Annually' as PurchaseFrequency,
  Occasional: 'Occasional' as PurchaseFrequency,
};

// Type definitions matching backend models
export interface Product {
  id: string;
  name: string;
  normalizedName: string;
  categoryId?: string;
  isManualCategory: boolean;
  averagePrice: number;
  frequency: PurchaseFrequency;
  lastPurchased?: string;
  notes?: string;
  tags?: string;
  createdBy: string;
  createdDate: string;
  frequencyPaused: boolean;
  category?: Category;
}

export interface Category {
  id: string;
  name: string;
  description?: string;
  isCustom: boolean;
  familyId?: string;
}

export interface ProductsListResponse {
  products: Product[];
  totalCount: number;
}

export interface ProductDetailsResponse {
  product: Product;
  purchaseHistory: PurchaseHistoryItem[];
}

export interface PurchaseHistoryItem {
  id: string;
  receiptId: string;
  productId: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  purchaseDate: string;
}

export interface SearchProductsRequest {
  searchTerm?: string;
  categoryId?: string;
  frequency?: PurchaseFrequency;
  uncategorizedOnly?: boolean;
  page?: number;
  pageSize?: number;
}

/**
 * Product API client for managing products and their attributes
 */
export const productApi = {
  /**
   * Get all products with optional filtering
   */
  async getAllProducts(params?: SearchProductsRequest): Promise<ProductsListResponse> {
    const response = await api.get('/products', { params });
    return response.data;
  },

  /**
   * Get a single product by ID with purchase history
   */
  async getProductById(productId: string): Promise<ProductDetailsResponse> {
    const response = await api.get(`/products/${productId}`);
    return response.data;
  },

  /**
   * Search products by name
   */
  async searchProducts(searchTerm: string): Promise<Product[]> {
    const response = await api.get('/products/search', {
      params: { searchTerm },
    });
    return response.data;
  },

  /**
   * Get uncategorized products (for bulk categorization)
   */
  async getUncategorizedProducts(): Promise<Product[]> {
    const response = await api.get('/products/uncategorized');
    return response.data;
  },

  /**
   * Get products by category ID
   */
  async getProductsByCategory(categoryId: string): Promise<Product[]> {
    const response = await api.get(`/products/category/${categoryId}`);
    return response.data;
  },

  /**
   * Get products by frequency (for shopping list generation)
   */
  async getProductsByFrequency(frequency: PurchaseFrequency): Promise<Product[]> {
    const response = await api.get('/products/frequency', {
      params: { frequency },
    });
    return response.data;
  },

  /**
   * Update product notes
   */
  async updateProductNotes(productId: string, notes: string): Promise<Product> {
    const response = await api.patch(`/products/${productId}/notes`, { notes });
    return response.data;
  },

  /**
   * Update product tags
   */
  async updateProductTags(productId: string, tags: string[]): Promise<Product> {
    const response = await api.patch(`/products/${productId}/tags`, { tags });
    return response.data;
  },
};

export default productApi;
