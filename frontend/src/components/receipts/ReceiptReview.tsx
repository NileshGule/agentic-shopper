import React, { useState, useEffect } from 'react';
import Button from '../common/Button';
import Input from '../common/Input';
import receiptApi, {
  ReceiptDetail,
  UpdateReceiptRequest,
} from '../../services/api/receiptApi';

interface ReceiptReviewProps {
  receiptId: string;
  onSaveSuccess?: () => void;
  onCancel?: () => void;
}

const ReceiptReview: React.FC<ReceiptReviewProps> = ({
  receiptId,
  onSaveSuccess,
  onCancel,
}) => {
  const [receipt, setReceipt] = useState<ReceiptDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Editable fields
  const [storeName, setStoreName] = useState('');
  const [purchaseDate, setPurchaseDate] = useState('');
  const [totalAmount, setTotalAmount] = useState('');
  const [purchases, setPurchases] = useState<
    Array<{
      id?: string;
      productName: string;
      quantity: number;
      unitPrice: number;
      totalPrice: number;
    }>
  >([]);

  useEffect(() => {
    loadReceipt();
  }, [receiptId]);

  const loadReceipt = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await receiptApi.getReceiptById(receiptId);
      setReceipt(data);

      // Initialize editable fields
      setStoreName(data.storeName ?? '');
      setPurchaseDate(data.purchaseDate.split('T')[0]); // Format to YYYY-MM-DD
      setTotalAmount(data.totalAmount.toString());
      setPurchases(
        data.purchases.map((p) => ({
          id: p.id,
          productName: p.productName ?? '',
          quantity: p.quantity,
          unitPrice: p.unitPrice,
          totalPrice: p.totalPrice,
        }))
      );
    } catch (err: any) {
      setError(
        err.response?.data?.error || err.message || 'Failed to load receipt'
      );
    } finally {
      setIsLoading(false);
    }
  };

  const handlePurchaseChange = (
    index: number,
    field: keyof typeof purchases[0],
    value: string | number
  ) => {
    const updatedPurchases = [...purchases];
    updatedPurchases[index] = {
      ...updatedPurchases[index],
      [field]: value,
    };

    // Recalculate total if quantity or unit price changed
    if (field === 'quantity' || field === 'unitPrice') {
      const qty = parseFloat(updatedPurchases[index].quantity.toString()) || 0;
      const price = parseFloat(updatedPurchases[index].unitPrice.toString()) || 0;
      updatedPurchases[index].totalPrice = qty * price;
    }

    setPurchases(updatedPurchases);
  };

  const handleAddPurchase = () => {
    setPurchases([
      ...purchases,
      {
        productName: '',
        quantity: 1,
        unitPrice: 0,
        totalPrice: 0,
      },
    ]);
  };

  const handleRemovePurchase = (index: number) => {
    setPurchases(purchases.filter((_, i) => i !== index));
  };

  const handleSave = async () => {
    setIsSaving(true);
    setError(null);

    try {
      const updateRequest: UpdateReceiptRequest = {
        storeName,
        purchaseDate: new Date(purchaseDate).toISOString(),
        totalAmount: parseFloat(totalAmount),
        purchases: purchases.map((p) => ({
          id: p.id,
          productName: p.productName,
          quantity: p.quantity,
          unitPrice: p.unitPrice,
        })),
      };

      await receiptApi.updateReceipt(receiptId, updateRequest);

      if (onSaveSuccess) {
        onSaveSuccess();
      }
    } catch (err: any) {
      setError(
        err.response?.data?.error || err.message || 'Failed to save changes'
      );
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <div className="receipt-review loading">
        <p>Loading receipt details...</p>
      </div>
    );
  }

  if (!receipt) {
    return (
      <div className="receipt-review error">
        <p>Receipt not found</p>
      </div>
    );
  }

  return (
    <div className="receipt-review">
      <div className="review-header">
        <h2>Review Receipt</h2>
        {receipt.status === 'NeedsReview' && (
          <span className="needs-review-badge">Needs Review</span>
        )}
        <p className="confidence-score">
          OCR Confidence: {(receipt.confidenceScore * 100).toFixed(1)}%
        </p>
      </div>

      <div className="review-form">
        {error && (
          <div className="error-message" role="alert">
            {error}
          </div>
        )}

        <div className="form-section">
          <h3>Receipt Information</h3>

          <div className="form-row">
            <label htmlFor="storeName">Store Name</label>
            <Input
              id="storeName"
              type="text"
              value={storeName}
              onChange={(e) => setStoreName(e.target.value)}
              placeholder="Enter store name"
            />
          </div>

          <div className="form-row">
            <label htmlFor="purchaseDate">Purchase Date</label>
            <Input
              id="purchaseDate"
              type="date"
              value={purchaseDate}
              onChange={(e) => setPurchaseDate(e.target.value)}
            />
          </div>

          <div className="form-row">
            <label htmlFor="totalAmount">Total Amount</label>
            <Input
              id="totalAmount"
              type="number"
              step="0.01"
              value={totalAmount}
              onChange={(e) => setTotalAmount(e.target.value)}
              placeholder="0.00"
            />
          </div>
        </div>

        <div className="form-section">
          <div className="section-header">
            <h3>Items ({purchases.length})</h3>
            <Button variant="secondary" onClick={handleAddPurchase} size="small">
              + Add Item
            </Button>
          </div>

          <div className="purchases-list">
            {purchases.map((purchase, index) => (
              <div key={index} className="purchase-item">
                <div className="purchase-fields">
                  <div className="field">
                    <label>Product Name</label>
                    <Input
                      type="text"
                      value={purchase.productName}
                      onChange={(e) =>
                        handlePurchaseChange(index, 'productName', e.target.value)
                      }
                      placeholder="Product name"
                    />
                  </div>

                  <div className="field field-small">
                    <label>Qty</label>
                    <Input
                      type="number"
                      step="0.1"
                      value={purchase.quantity}
                      onChange={(e) =>
                        handlePurchaseChange(
                          index,
                          'quantity',
                          parseFloat(e.target.value) || 0
                        )
                      }
                    />
                  </div>

                  <div className="field">
                    <label>Unit Price</label>
                    <Input
                      type="number"
                      step="0.01"
                      value={purchase.unitPrice}
                      onChange={(e) =>
                        handlePurchaseChange(
                          index,
                          'unitPrice',
                          parseFloat(e.target.value) || 0
                        )
                      }
                    />
                  </div>

                  <div className="field">
                    <label>Total</label>
                    <Input
                      type="number"
                      step="0.01"
                      value={purchase.totalPrice.toFixed(2)}
                      readOnly
                      disabled
                    />
                  </div>

                  <div className="field-actions">
                    <Button
                      variant="danger"
                      size="small"
                      onClick={() => handleRemovePurchase(index)}
                    >
                      ✕
                    </Button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="form-actions">
          <Button
            variant="primary"
            onClick={handleSave}
            isLoading={isSaving}
            disabled={isSaving}
          >
            Save Changes
          </Button>
          {onCancel && (
            <Button variant="secondary" onClick={onCancel} disabled={isSaving}>
              Cancel
            </Button>
          )}
        </div>
      </div>

      <style>{`
        .receipt-review {
          max-width: 900px;
          margin: 0 auto;
        }

        .receipt-review.loading,
        .receipt-review.error {
          padding: 2rem;
          text-align: center;
        }

        .review-header {
          margin-bottom: 2rem;
        }

        .review-header h2 {
          margin: 0 0 0.5rem 0;
        }

        .needs-review-badge {
          display: inline-block;
          padding: 0.25rem 0.75rem;
          background: #fed7d7;
          color: #c53030;
          border-radius: 4px;
          font-size: 0.875rem;
          font-weight: 500;
          margin-left: 1rem;
        }

        .confidence-score {
          color: #718096;
          font-size: 0.875rem;
          margin: 0.5rem 0 0 0;
        }

        .review-form {
          background: white;
          border-radius: 8px;
          padding: 2rem;
          box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        }

        .form-section {
          margin-bottom: 2rem;
        }

        .form-section:last-of-type {
          margin-bottom: 0;
        }

        .form-section h3 {
          margin: 0 0 1rem 0;
          font-size: 1.25rem;
        }

        .section-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 1rem;
        }

        .section-header h3 {
          margin: 0;
        }

        .form-row {
          margin-bottom: 1rem;
        }

        .form-row label {
          display: block;
          font-weight: 500;
          margin-bottom: 0.5rem;
          color: #2d3748;
        }

        .error-message {
          padding: 1rem;
          background: #fed7d7;
          color: #c53030;
          border-radius: 4px;
          margin-bottom: 1.5rem;
        }

        .purchases-list {
          display: flex;
          flex-direction: column;
          gap: 1rem;
        }

        .purchase-item {
          padding: 1rem;
          border: 1px solid #e2e8f0;
          border-radius: 8px;
          background: #f7fafc;
        }

        .purchase-fields {
          display: grid;
          grid-template-columns: 2fr 0.7fr 1fr 1fr auto;
          gap: 1rem;
          align-items: end;
        }

        .field label {
          display: block;
          font-size: 0.875rem;
          font-weight: 500;
          margin-bottom: 0.25rem;
          color: #4a5568;
        }

        .field-small {
          max-width: 100px;
        }

        .field-actions {
          display: flex;
          align-items: flex-end;
        }

        .form-actions {
          display: flex;
          gap: 1rem;
          margin-top: 2rem;
          padding-top: 2rem;
          border-top: 1px solid #e2e8f0;
        }

        @media (max-width: 768px) {
          .purchase-fields {
            grid-template-columns: 1fr;
          }

          .field-small {
            max-width: none;
          }
        }
      `}</style>
    </div>
  );
};

export default ReceiptReview;
