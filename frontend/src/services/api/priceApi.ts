import api from './client';

// Types matching the backend DTOs

export interface ProductQueryDto {
  productName: string;
  quantity: number;
}

export interface PriceComparisonRequest {
  products: ProductQueryDto[];
}

export interface PromotionDto {
  id: string;
  productName: string;
  storeName: 'Coles' | 'Woolworths';
  originalPrice: number;
  salePrice: number;
  discountPercentage: number;
  startDate: string;
  endDate: string;
  catalogWeek: string;
}

export interface PaginationDto {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface CurrentPromotionsResponse {
  promotions: PromotionDto[];
  pagination: PaginationDto;
}

export interface ProductPriceComparisonDto {
  productName: string;
  quantity: number;
  colesPrice?: number;
  woolworthsPrice?: number;
  colesSalePrice?: number;
  woolworthsSalePrice?: number;
  hasColesPromotion: boolean;
  hasWoolworthsPromotion: boolean;
  bestPrice?: number;
  bestStore: string;
  savings: number;
}

export interface SplitStrategyDto {
  colesPurchases: string[];
  woolworthsPurchases: string[];
  totalSavings: number;
}

export interface PricingSummaryDto {
  colesTotal: number;
  woolworthsTotal: number;
  potentialSavings: number;
  recommendedStore: string;
  splitStrategy?: SplitStrategyDto;
}

export interface PriceComparisonResponse {
  items: ProductPriceComparisonDto[];
  summary: PricingSummaryDto;
}

export interface RefreshJobResponse {
  jobId: string;
  status: string;
  promotionsAdded: number;
}

// API Service

const priceApi = {
  /**
   * Get current promotions with optional filtering
   */
  async getCurrentPromotions(params?: {
    store?: 'Coles' | 'Woolworths' | 'All';
    productName?: string;
    page?: number;
    pageSize?: number;
  }): Promise<CurrentPromotionsResponse> {
    const queryParams = new URLSearchParams();

    if (params?.store) {
      queryParams.append('store', params.store);
    }
    if (params?.productName) {
      queryParams.append('productName', params.productName);
    }
    if (params?.page) {
      queryParams.append('page', params.page.toString());
    }
    if (params?.pageSize) {
      queryParams.append('pageSize', params.pageSize.toString());
    }

    const response = await api.get<CurrentPromotionsResponse>(
      `/api/v1/promotions/current?${queryParams.toString()}`
    );

    return response.data;
  },

  /**
   * Compare prices across stores for shopping list items
   */
  async comparePrices(
    request: PriceComparisonRequest
  ): Promise<PriceComparisonResponse> {
    const response = await api.post<PriceComparisonResponse>(
      '/api/v1/prices/compare',
      request
    );

    return response.data;
  },

  /**
   * Compare prices for a single shopping list
   * Convenience method that converts shopping list items to price comparison format
   */
  async comparePricesForList(items: {
    productName: string;
    quantity: number;
  }[]): Promise<PriceComparisonResponse> {
    const request: PriceComparisonRequest = {
      products: items.map((item) => ({
        productName: item.productName,
        quantity: item.quantity,
      })),
    };

    return this.comparePrices(request);
  },

  /**
   * Trigger manual promotion data refresh (admin only)
   */
  async refreshPromotions(): Promise<RefreshJobResponse> {
    const response = await api.post<RefreshJobResponse>(
      '/api/v1/promotions/refresh'
    );

    return response.data;
  },

  /**
   * Search promotions by product name
   */
  async searchPromotions(
    productName: string,
    store?: 'Coles' | 'Woolworths' | 'All'
  ): Promise<PromotionDto[]> {
    const result = await this.getCurrentPromotions({
      productName,
      store,
      pageSize: 50,
    });

    return result.promotions;
  },

  /**
   * Get promotions for a specific store
   */
  async getPromotionsByStore(
    store: 'Coles' | 'Woolworths',
    page: number = 1,
    pageSize: number = 50
  ): Promise<CurrentPromotionsResponse> {
    return this.getCurrentPromotions({
      store,
      page,
      pageSize,
    });
  },

  /**
   * Calculate best shopping option for a list of products
   */
  async getBestShoppingOption(
    products: { productName: string; quantity: number }[]
  ): Promise<{
    recommendedStore: string;
    totalSavings: number;
    shouldSplit: boolean;
    splitStrategy?: SplitStrategyDto;
  }> {
    const comparison = await this.comparePricesForList(products);

    return {
      recommendedStore: comparison.summary.recommendedStore,
      totalSavings: comparison.summary.potentialSavings,
      shouldSplit:
        comparison.summary.splitStrategy !== undefined &&
        comparison.summary.splitStrategy !== null,
      splitStrategy: comparison.summary.splitStrategy,
    };
  },
};

export default priceApi;
