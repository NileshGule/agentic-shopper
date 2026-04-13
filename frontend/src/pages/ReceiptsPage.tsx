import { useState } from 'react';
import ReceiptUpload from '../components/receipts/ReceiptUpload';
import ReceiptList from '../components/receipts/ReceiptList';
import ReceiptReview from '../components/receipts/ReceiptReview';
import Button from '../components/common/Button';
import { useToast } from '../components/common/ToastContext';
import { DEMO_FAMILY_ID, DEMO_USER_ID } from '../constants/demo';

type ViewMode = 'list' | 'upload' | 'review';

export default function ReceiptsPage() {
  const [viewMode, setViewMode] = useState<ViewMode>('list');
  const [selectedReceiptId, setSelectedReceiptId] = useState<string | null>(null);
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const toast = useToast();

  // TODO: Replace with actual family ID from user context/auth
  const familyId = DEMO_FAMILY_ID;
  const uploadedBy = DEMO_USER_ID;

  const handleUploadSuccess = (receiptId: string) => {
    // Automatically switch to review mode for new receipts
    setSelectedReceiptId(receiptId);
    setViewMode('review');
    setRefreshTrigger((prev) => prev + 1);
    toast.showSuccess('Receipt uploaded successfully! Please review the extracted data.');
  };

  const handleSelectReceipt = (receiptId: string) => {
    setSelectedReceiptId(receiptId);
    setViewMode('review');
  };

  const handleSaveSuccess = () => {
    setViewMode('list');
    setSelectedReceiptId(null);
    setRefreshTrigger((prev) => prev + 1);
    toast.showSuccess('Receipt saved successfully!');
  };

  const handleCancel = () => {
    setViewMode('list');
    setSelectedReceiptId(null);
  };

  const handleUploadError = (error: string) => {
    console.error('Upload error:', error);
    toast.showError(error || 'Failed to upload receipt. Please try again.');
  };

  return (
    <div className="receipts-page">
      <div className="page-header">
        <h1>Receipt Management</h1>
        <div className="header-actions">
          {viewMode !== 'upload' && (
            <Button
              variant="primary"
              onClick={() => setViewMode('upload')}
            >
              📸 Upload Receipt
            </Button>
          )}
          {viewMode !== 'list' && (
            <Button
              variant="secondary"
              onClick={() => setViewMode('list')}
            >
              📋 View All
            </Button>
          )}
        </div>
      </div>

      <div className="page-content">
        {viewMode === 'upload' && (
          <ReceiptUpload
            familyId={familyId}
            uploadedBy={uploadedBy}
            onUploadSuccess={handleUploadSuccess}
            onUploadError={handleUploadError}
          />
        )}

        {viewMode === 'list' && (
          <ReceiptList
            familyId={familyId}
            onSelectReceipt={handleSelectReceipt}
            onRefresh={refreshTrigger > 0 ? () => {} : undefined}
          />
        )}

        {viewMode === 'review' && selectedReceiptId && (
          <ReceiptReview
            receiptId={selectedReceiptId}
            onSaveSuccess={handleSaveSuccess}
            onCancel={handleCancel}
          />
        )}
      </div>

      <style>{`
        .receipts-page {
          padding: 2rem;
          min-height: 100vh;
          background: #f7fafc;
        }

        .page-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 2rem;
        }

        .page-header h1 {
          margin: 0;
          font-size: 2rem;
          color: #2d3748;
        }

        .header-actions {
          display: flex;
          gap: 1rem;
        }

        .page-content {
          margin-top: 2rem;
        }

        @media (max-width: 768px) {
          .receipts-page {
            padding: 1rem;
          }

          .page-header {
            flex-direction: column;
            gap: 1rem;
            align-items: stretch;
          }

          .page-header h1 {
            font-size: 1.5rem;
          }

          .header-actions {
            justify-content: center;
          }
        }
      `}</style>
    </div>
  );
}

