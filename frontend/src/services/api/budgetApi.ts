import api from './client';

export type BudgetPeriod = 'Weekly' | 'Monthly';

export interface CreateBudgetDto {
  familyId: string;
  categoryId: string;
  amount: number;
  period: BudgetPeriod;
  alertThreshold?: number; // Default 0.9 (90%)
}

export interface UpdateBudgetDto {
  amount?: number;
  alertThreshold?: number;
}

export interface BudgetDto {
  id: string;
  familyId: string;
  categoryId: string;
  categoryName: string;
  amount: number;
  currentSpent: number;
  remaining: number;
  percentageUsed: number;
  period: BudgetPeriod;
  startDate: string;
  endDate: string;
  alertThreshold: number;
  lastAlertSent?: string;
  status: string; // "On Track" | "Warning" | "Over Budget"
}

export interface BudgetSummary {
  budgetId: string;
  categoryName: string;
  amount: number;
  currentSpent: number;
  remaining: number;
  percentageUsed: number;
  period: BudgetPeriod;
  startDate: string;
  endDate: string;
  status: string;
}

export interface BudgetAlert {
  budgetId: string;
  categoryName: string;
  amount: number;
  currentSpent: number;
  alertThreshold: number;
  percentageUsed: number;
  remaining: number;
  isOverBudget: boolean;
  periodStart: string;
  periodEnd: string;
  message: string;
}

export interface BudgetStatusResponse {
  budgets: BudgetSummary[];
  alerts: BudgetAlert[];
  totalBudgeted: number;
  totalSpent: number;
}

export interface AnalyticsRequestDto {
  startDate?: string;
  endDate?: string;
  trendType?: string; // "weekly" | "monthly" | "quarterly"
}

export interface DataPoint {
  date: string;
  value: number;
  label: string; // "Week of Mar 15", "Q1 2025", etc.
}

export interface SpendingAnalyticsResponse {
  trendData: DataPoint[];
  categoryBreakdown: Record<string, number>;
  storeBreakdown: Record<string, number>;
  totalSpent: number;
  transactionCount: number;
  averagePerTransaction: number;
}

/**
 * API client for budget tracking and analytics
 */
export const budgetApi = {
  /**
   * Create a new budget (T136)
   */
  async createBudget(budget: CreateBudgetDto): Promise<BudgetDto> {
    const response = await api.post<BudgetDto>('/api/v1/budgets', budget);
    return response.data;
  },

  /**
   * Get a specific budget by ID
   */
  async getBudgetById(budgetId: string): Promise<BudgetDto> {
    const response = await api.get<BudgetDto>(`/api/v1/budgets/${budgetId}`);
    return response.data;
  },

  /**
   * Get all budgets for a family
   */
  async getFamilyBudgets(familyId: string): Promise<BudgetDto[]> {
    const response = await api.get<BudgetDto[]>(
      `/api/v1/budgets/family/${familyId}`
    );
    return response.data;
  },

  /**
   * Update an existing budget
   */
  async updateBudget(
    budgetId: string,
    updates: UpdateBudgetDto
  ): Promise<BudgetDto> {
    const response = await api.put<BudgetDto>(
      `/api/v1/budgets/${budgetId}`,
      updates
    );
    return response.data;
  },

  /**
   * Delete a budget
   */
  async deleteBudget(budgetId: string): Promise<void> {
    await api.delete(`/api/v1/budgets/${budgetId}`);
  },

  /**
   * Get budget status with alerts (T136)
   */
  async getBudgetStatus(familyId: string): Promise<BudgetStatusResponse> {
    const response = await api.get<BudgetStatusResponse>(
      `/api/v1/budgets/family/${familyId}/status`
    );
    return response.data;
  },

  /**
   * Get spending analytics (T136)
   */
  async getSpendingAnalytics(
    familyId: string,
    request: AnalyticsRequestDto
  ): Promise<SpendingAnalyticsResponse> {
    const response = await api.post<SpendingAnalyticsResponse>(
      `/api/v1/budgets/family/${familyId}/analytics`,
      request
    );
    return response.data;
  },

  /**
   * Get active alerts for a family
   */
  async getActiveAlerts(familyId: string): Promise<BudgetAlert[]> {
    const response = await api.get<BudgetAlert[]>(
      `/api/v1/budgets/family/${familyId}/alerts`
    );
    return response.data;
  },

  /**
   * Recalculate all budgets from purchase data
   */
  async recalculateBudgets(familyId: string): Promise<void> {
    await api.post(`/api/v1/budgets/family/${familyId}/recalculate`);
  }
};
