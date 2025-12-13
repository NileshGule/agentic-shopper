import api from './client';

export interface Category {
  id: string;
  name: string;
  description?: string;
  isCustom: boolean;
}

export interface SuggestCategoryRequest {
  productName: string;
  storeName?: string;
}

export interface CategorizationResponse {
  productId: string;
  categoryId: string;
  categoryName: string;
  confidence: number;
  isManualOverride: boolean;
  suggestedAt?: string;
}

export interface AssignCategoryRequest {
  productId: string;
  categoryId: string;
}

export interface BatchCategorizationRequest {
  productIds: string[];
  forceRecategorize: boolean;
}

/**
 * API client for product categorization operations
 */
export const categorizationApi = {
  /**
   * Get all available categories (predefined + custom)
   */
  async getCategories(): Promise<Category[]> {
    const response = await api.get<Category[]>('/api/categorization/categories');
    return response.data;
  },

  /**
   * Suggest a category for a product name (without saving)
   */
  async suggestCategory(request: SuggestCategoryRequest): Promise<CategorizationResponse> {
    const response = await api.post<CategorizationResponse>(
      '/api/categorization/suggest',
      request
    );
    return response.data;
  },

  /**
   * Automatically categorize a single product
   */
  async categorizeProduct(
    productId: string,
    forceRecategorize: boolean = false
  ): Promise<CategorizationResponse> {
    const response = await api.post<CategorizationResponse>(
      `/api/categorization/categorize/${productId}`,
      null,
      {
        params: { forceRecategorize }
      }
    );
    return response.data;
  },

  /**
   * Batch categorize multiple products
   */
  async categorizeBatch(
    request: BatchCategorizationRequest
  ): Promise<CategorizationResponse[]> {
    const response = await api.post<CategorizationResponse[]>(
      '/api/categorization/categorize/batch',
      request
    );
    return response.data;
  },

  /**
   * Manually assign a category to a product (FR-009)
   */
  async assignCategory(request: AssignCategoryRequest): Promise<CategorizationResponse> {
    const response = await api.post<CategorizationResponse>(
      '/api/categorization/assign',
      request
    );
    return response.data;
  },

  /**
   * Get categorization history for a product
   */
  async getCategorizationHistory(productId: string): Promise<CategorizationResponse[]> {
    const response = await api.get<CategorizationResponse[]>(
      `/api/categorization/history/${productId}`
    );
    return response.data;
  }
};
