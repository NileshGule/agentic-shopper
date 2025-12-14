import React, { useState, useEffect } from 'react';
import Button from '../common/Button';
import { ReceiptCardSkeleton } from '../common/SkeletonLoader';
import receiptApi, { ReceiptSummary } from '../../services/api/receiptApi';

interface ReceiptListProps {
  familyId: string;
  onSelectReceipt?: (receiptId: string) => void;
  onRefresh?: () => void;
}

const ReceiptList: React.FC<ReceiptListProps> = ({
  familyId,
  onSelectReceipt,
  onRefresh,
}) => {
  const [receipts, setReceipts] = useState<ReceiptSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<'all' | 'needsReview'>('all');
  const [isDeleting, setIsDeleting] = useState<string | null>(null);

  useEffect(() => {
    loadReceipts();
  }, [familyId, filter]);

  useEffect(() => {
    if (onRefresh) {
      loadReceipts();
    }
  }, [onRefresh]);

  const loadReceipts = async () => {
    setIsLoading(true);
    setError(null);

    try {
      let data: ReceiptSummary[];
      if (filter === 'needsReview') {
        data = await receiptApi.getReceiptsNeedingReview(familyId);
      } else {
        data = await receiptApi.getReceiptsByFamily(familyId);
      }

      // Sort by purchase date (newest first)
      data.sort(
        (a, b) =>
          new Date(b.purchaseDate).getTime() - new Date(a.purchaseDate).getTime()
      );

      setReceipts(data);
    } catch (err: any) {
      setError(
        err.response?.data?.error || err.message || 'Failed to load receipts'
      );
    } finally {
      setIsLoading(false);
    }
  };

  const handleDelete = async (receiptId: string) => {
    if (!confirm('Are you sure you want to delete this receipt?')) {
      return;
    }

    setIsDeleting(receiptId);
    setError(null);

    try {
      await receiptApi.deleteReceipt(receiptId);
      setReceipts(receipts.filter((r) => r.id !== receiptId));
    } catch (err: any) {
      setError(
        err.response?.data?.error || err.message || 'Failed to delete receipt'
      );
    } finally {
      setIsDeleting(null);
    }
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    }).format(date);
  };

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const getStatusBadgeClass = (status: string): string => {
    switch (status) {
      case 'Verified':
        return 'badge-verified';
      case 'NeedsReview':
        return 'badge-needs-review';
      case 'Pending':
        return 'badge-pending';
      default:
        return 'badge-default';
    }
  };

  const getStatusLabel = (status: string): string => {
    switch (status) {
      case 'Verified':
        return '✓ Verified';
      case 'NeedsReview':
        return '⚠ Needs Review';
      case 'Pending':
        return '⏳ Pending';
      default:
        return status;
    }
  };

  if (isLoading) {
    return (
      <div className="receipt-list">
        <div className="list-header">
          <div className="filter-tabs">
            <button className="tab active">All Receipts</button>
            <button className="tab">Needs Review</button>
          </div>
        </div>
        <div className="skeleton-container" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '16px', padding: '20px' }}>
          <ReceiptCardSkeleton />
          <ReceiptCardSkeleton />
          <ReceiptCardSkeleton />
          <ReceiptCardSkeleton />
          <ReceiptCardSkeleton />
          <ReceiptCardSkeleton />
        </div>
      </div>
    );
  }

  return (
    <div className="receipt-list">
      <div className="list-header">
        <div className="filter-tabs">
          <button
            className={`tab ${filter === 'all' ? 'active' : ''}`}
            onClick={() => setFilter('all')}
          >
            All Receipts
          </button>
          <button
            className={`tab ${filter === 'needsReview' ? 'active' : ''}`}
            onClick={() => setFilter('needsReview')}
          >
            Needs Review
          </button>
        </div>

        <Button variant="secondary" onClick={loadReceipts} size="small">
          🔄 Refresh
        </Button>
      </div>

      {error && (
        <div className="error-message" role="alert">
          {error}
        </div>
      )}

      {receipts.length === 0 ? (
        <div className="empty-state">
          <p>
            {filter === 'needsReview'
              ? 'No receipts need review'
              : 'No receipts found'}
          </p>
          <p className="empty-hint">
            {filter === 'all' && 'Upload a receipt to get started'}
          </p>
        </div>
      ) : (
        <div className="receipts-grid">
          {receipts.map((receipt) => (
            <div key={receipt.id} className="receipt-card">
              <div className="card-header">
                <h3 className="store-name">{receipt.storeName}</h3>
                <span className={`status-badge ${getStatusBadgeClass(receipt.status)}`}>
                  {getStatusLabel(receipt.status)}
                </span>
              </div>

              <div className="card-body">
                <div className="receipt-info">
                  <div className="info-row">
                    <span className="label">Date:</span>
                    <span className="value">{formatDate(receipt.purchaseDate)}</span>
                  </div>

                  <div className="info-row">
                    <span className="label">Total:</span>
                    <span className="value amount">
                      {formatCurrency(receipt.totalAmount)}
                    </span>
                  </div>

                  <div className="info-row">
                    <span className="label">Items:</span>
                    <span className="value">{receipt.itemCount}</span>
                  </div>

                  <div className="info-row">
                    <span className="label">Confidence:</span>
                    <span className="value">
                      {(receipt.confidenceScore * 100).toFixed(0)}%
                    </span>
                  </div>
                </div>
              </div>

              <div className="card-footer">
                <Button
                  variant="primary"
                  onClick={() => onSelectReceipt && onSelectReceipt(receipt.id)}
                  size="small"
                >
                  {receipt.status === 'NeedsReview' ? 'Review' : 'View'}
                </Button>
                <Button
                  variant="danger"
                  onClick={() => handleDelete(receipt.id)}
                  isLoading={isDeleting === receipt.id}
                  disabled={isDeleting === receipt.id}
                  size="small"
                >
                  Delete
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      <style>{`
        .receipt-list {
          max-width: 1200px;
          margin: 0 auto;
        }

        .receipt-list.loading {
          padding: 2rem;
          text-align: center;
        }

        .list-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 2rem;
        }

        .filter-tabs {
          display: flex;
          gap: 0.5rem;
        }

        .tab {
          padding: 0.5rem 1.5rem;
          border: none;
          background: white;
          border-radius: 8px;
          font-size: 1rem;
          cursor: pointer;
          transition: all 0.2s;
          box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
        }

        .tab:hover {
          background: #f7fafc;
        }

        .tab.active {
          background: #4299e1;
          color: white;
          font-weight: 500;
        }

        .error-message {
          padding: 1rem;
          background: #fed7d7;
          color: #c53030;
          border-radius: 4px;
          margin-bottom: 1.5rem;
        }

        .empty-state {
          text-align: center;
          padding: 4rem 2rem;
          background: white;
          border-radius: 8px;
          box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
        }

        .empty-state p {
          margin: 0.5rem 0;
          color: #718096;
        }

        .empty-hint {
          font-size: 0.875rem;
        }

        .receipts-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
          gap: 1.5rem;
        }

        .receipt-card {
          background: white;
          border-radius: 8px;
          box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
          overflow: hidden;
          transition: transform 0.2s, box-shadow 0.2s;
        }

        .receipt-card:hover {
          transform: translateY(-2px);
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        }

        .card-header {
          padding: 1.25rem;
          border-bottom: 1px solid #e2e8f0;
          display: flex;
          justify-content: space-between;
          align-items: start;
          gap: 1rem;
        }

        .store-name {
          margin: 0;
          font-size: 1.125rem;
          font-weight: 600;
          color: #2d3748;
          flex: 1;
        }

        .status-badge {
          display: inline-block;
          padding: 0.25rem 0.75rem;
          border-radius: 12px;
          font-size: 0.75rem;
          font-weight: 500;
          white-space: nowrap;
        }

        .badge-verified {
          background: #c6f6d5;
          color: #22543d;
        }

        .badge-needs-review {
          background: #fed7d7;
          color: #c53030;
        }

        .badge-pending {
          background: #feebc8;
          color: #7c2d12;
        }

        .badge-default {
          background: #e2e8f0;
          color: #2d3748;
        }

        .card-body {
          padding: 1.25rem;
        }

        .receipt-info {
          display: flex;
          flex-direction: column;
          gap: 0.75rem;
        }

        .info-row {
          display: flex;
          justify-content: space-between;
          align-items: center;
        }

        .label {
          color: #718096;
          font-size: 0.875rem;
        }

        .value {
          font-weight: 500;
          color: #2d3748;
        }

        .value.amount {
          font-size: 1.125rem;
          color: #2b6cb0;
        }

        .card-footer {
          padding: 1rem 1.25rem;
          background: #f7fafc;
          display: flex;
          gap: 0.75rem;
        }

        @media (max-width: 768px) {
          .receipts-grid {
            grid-template-columns: 1fr;
          }

          .list-header {
            flex-direction: column;
            gap: 1rem;
            align-items: stretch;
          }

          .filter-tabs {
            justify-content: center;
          }
        }
      `}</style>
    </div>
  );
};

export default ReceiptList;
