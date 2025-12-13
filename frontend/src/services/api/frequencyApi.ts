import api from './client';

export type PurchaseFrequency =
  | 'Unknown'
  | 'Weekly'
  | 'Fortnightly'
  | 'Monthly'
  | 'Quarterly'
  | 'Annually'
  | 'Occasional';

export interface FrequencyCalculationResponse {
  productId: string;
  frequency: PurchaseFrequency;
  averageDaysBetween: number;
  purchaseCount: number;
  lastCalculated: string;
  nextExpectedDate?: string;
  confidence: number;
}

export interface FrequencyOverrideRequest {
  productId: string;
  frequency: PurchaseFrequency;
}

export interface BatchFrequencyRequest {
  productIds: string[];
  forceRecalculate: boolean;
}

export interface FrequencyStatusResponse {
  productId: string;
  frequency: PurchaseFrequency;
  isPaused: boolean;
  pausedAt?: string;
  pausedBy?: string;
}

/**
 * API client for purchase frequency operations
 */
export const frequencyApi = {
  /**
   * Calculate purchase frequency for a single product
   */
  async calculateFrequency(
    productId: string,
    forceRecalculate: boolean = false
  ): Promise<FrequencyCalculationResponse> {
    const response = await api.post<FrequencyCalculationResponse>(
      `/api/frequency/calculate/${productId}`,
      null,
      {
        params: { forceRecalculate }
      }
    );
    return response.data;
  },

  /**
   * Calculate frequency for multiple products in batch
   */
  async calculateBatchFrequency(
    request: BatchFrequencyRequest
  ): Promise<FrequencyCalculationResponse[]> {
    const response = await api.post<FrequencyCalculationResponse[]>(
      '/api/frequency/calculate/batch',
      request
    );
    return response.data;
  },

  /**
   * Manually override frequency for a product (FR-011)
   */
  async overrideFrequency(
    request: FrequencyOverrideRequest
  ): Promise<FrequencyCalculationResponse> {
    const response = await api.post<FrequencyCalculationResponse>(
      '/api/frequency/override',
      request
    );
    return response.data;
  },

  /**
   * Pause frequency tracking for a product (FR-015 - vacation mode)
   */
  async pauseFrequency(productId: string): Promise<FrequencyStatusResponse> {
    const response = await api.post<FrequencyStatusResponse>(
      `/api/frequency/pause/${productId}`
    );
    return response.data;
  },

  /**
   * Resume frequency tracking for a product
   */
  async resumeFrequency(productId: string): Promise<FrequencyStatusResponse> {
    const response = await api.post<FrequencyStatusResponse>(
      `/api/frequency/resume/${productId}`
    );
    return response.data;
  },

  /**
   * Get frequency status for a product
   */
  async getFrequencyStatus(productId: string): Promise<FrequencyStatusResponse> {
    const response = await api.get<FrequencyStatusResponse>(
      `/api/frequency/status/${productId}`
    );
    return response.data;
  },

  /**
   * Get frequency history for a product
   */
  async getFrequencyHistory(productId: string): Promise<FrequencyCalculationResponse[]> {
    const response = await api.get<FrequencyCalculationResponse[]>(
      `/api/frequency/history/${productId}`
    );
    return response.data;
  },

  /**
   * Mark product as purchased outside system (FR-021)
   * Updates last purchase date without creating a receipt
   */
  async markAsPurchasedExternally(
    productId: string,
    purchaseDate: Date
  ): Promise<FrequencyCalculationResponse> {
    const response = await api.post<FrequencyCalculationResponse>(
      `/api/frequency/${productId}/mark-purchased`,
      { purchaseDate: purchaseDate.toISOString() }
    );
    return response.data;
  }
};
