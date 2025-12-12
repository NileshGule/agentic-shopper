import apiClient from './client';

export interface UploadReceiptResponse {
  receiptId: string;
  storeName?: string;
  purchaseDate: string;
  totalAmount: number;
  itemCount: number;
  confidenceScore: number;
  status?: string;
  needsReview: boolean;
  message?: string;
}

export interface ReceiptSummary {
  id: string;
  storeName?: string;
  purchaseDate: string;
  totalAmount: number;
  itemCount: number;
  confidenceScore: number;
  status?: string;
  createdDate: string;
  imageUrl?: string;
}

export interface PurchaseItem {
  id: string;
  productId: string;
  productName?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  categoryName?: string;
}

export interface ReceiptDetail {
  id: string;
  familyId: string;
  uploadedBy: string;
  storeName?: string;
  purchaseDate: string;
  totalAmount: number;
  imageUrl?: string;
  confidenceScore: number;
  status?: string;
  createdDate: string;
  verifiedDate?: string;
  purchases: PurchaseItem[];
}

export interface UpdateReceiptRequest {
  storeName?: string;
  purchaseDate?: string;
  totalAmount?: number;
  status?: string;
  purchases?: Array<{
    id?: string;
    productName?: string;
    quantity?: number;
    unitPrice?: number;
    totalPrice?: number;
  }>;
}

/**
 * Receipt API service
 */
const receiptApi = {
  /**
   * Upload a receipt image
   */
  async uploadReceipt(
    file: File,
    familyId: string,
    uploadedBy: string
  ): Promise<UploadReceiptResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('familyId', familyId);
    formData.append('uploadedBy', uploadedBy);

    const response = await apiClient.post<UploadReceiptResponse>(
      '/api/receipts/upload',
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );

    return response.data;
  },

  /**
   * Get all receipts for a family
   */
  async getReceiptsByFamily(familyId: string): Promise<ReceiptSummary[]> {
    const response = await apiClient.get<ReceiptSummary[]>(
      `/api/receipts/family/${familyId}`
    );
    return response.data;
  },

  /**
   * Get receipt details by ID
   */
  async getReceiptById(id: string): Promise<ReceiptDetail> {
    const response = await apiClient.get<ReceiptDetail>(`/api/receipts/${id}`);
    return response.data;
  },

  /**
   * Update receipt details
   */
  async updateReceipt(
    id: string,
    updates: UpdateReceiptRequest
  ): Promise<ReceiptDetail> {
    const response = await apiClient.put<ReceiptDetail>(
      `/api/receipts/${id}`,
      updates
    );
    return response.data;
  },

  /**
   * Delete a receipt
   */
  async deleteReceipt(id: string): Promise<void> {
    await apiClient.delete(`/api/receipts/${id}`);
  },

  /**
   * Get receipts needing review
   */
  async getReceiptsNeedingReview(familyId: string): Promise<ReceiptSummary[]> {
    const response = await apiClient.get<ReceiptSummary[]>(
      `/api/receipts/family/${familyId}/needs-review`
    );
    return response.data;
  },
};

export default receiptApi;
