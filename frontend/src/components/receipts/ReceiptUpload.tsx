import React, { useState } from 'react';
import Button from '../common/Button';

interface ReceiptUploadProps {
  familyId: string;
  uploadedBy: string;
  onUploadSuccess?: (receiptId: string) => void;
  onUploadError?: (error: string) => void;
}

const ReceiptUpload: React.FC<ReceiptUploadProps> = ({
  familyId,
  uploadedBy,
  onUploadSuccess,
  onUploadError,
}) => {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'application/pdf'];
    if (!allowedTypes.includes(file.type)) {
      setError('Invalid file type. Please upload JPEG, PNG, or PDF files only.');
      return;
    }

    // Validate file size (10MB max)
    const maxSize = 10 * 1024 * 1024;
    if (file.size > maxSize) {
      setError('File size exceeds 10MB limit.');
      return;
    }

    setSelectedFile(file);
    setError(null);

    // Create preview for images
    if (file.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.onloadend = () => {
        setPreviewUrl(reader.result as string);
      };
      reader.readAsDataURL(file);
    } else {
      setPreviewUrl(null);
    }
  };

  const handleUpload = async () => {
    if (!selectedFile) {
      setError('Please select a file first.');
      return;
    }

    setIsUploading(true);
    setUploadProgress(0);
    setError(null);

    try {
      // Import receiptApi dynamically to avoid circular dependencies
      const { default: receiptApi } = await import('../../services/api/receiptApi');

      // Simulate progress (since we don't have real progress from the API)
      const progressInterval = setInterval(() => {
        setUploadProgress((prev) => Math.min(prev + 10, 90));
      }, 200);

      const response = await receiptApi.uploadReceipt(
        selectedFile,
        familyId,
        uploadedBy
      );

      clearInterval(progressInterval);
      setUploadProgress(100);

      // Reset form
      setSelectedFile(null);
      setPreviewUrl(null);

      // Notify parent
      if (onUploadSuccess) {
        onUploadSuccess(response.receiptId);
      }

      // Show success message if needs review
      if (response.needsReview) {
        setError(
          'Receipt uploaded but needs manual review due to low OCR confidence.'
        );
      }
    } catch (err: any) {
      setError(
        err.response?.data?.error ||
          err.message ||
          'Failed to upload receipt. Please try again.'
      );
      if (onUploadError) {
        onUploadError(error || 'Upload failed');
      }
    } finally {
      setIsUploading(false);
      setUploadProgress(0);
    }
  };

  const handleClear = () => {
    setSelectedFile(null);
    setPreviewUrl(null);
    setError(null);
    setUploadProgress(0);
  };

  return (
    <div className="receipt-upload">
      <div className="upload-container">
        <div className="upload-area">
          <input
            type="file"
            id="receipt-file"
            accept="image/jpeg,image/jpg,image/png,application/pdf"
            onChange={handleFileSelect}
            disabled={isUploading}
            className="file-input"
          />
          <label htmlFor="receipt-file" className="file-label">
            {selectedFile ? (
              <span>Selected: {selectedFile.name}</span>
            ) : (
              <>
                <span className="upload-icon">📁</span>
                <span>Click to select receipt image</span>
                <span className="file-hint">JPEG, PNG, or PDF (max 10MB)</span>
              </>
            )}
          </label>
        </div>

        {previewUrl && (
          <div className="preview-container">
            <img src={previewUrl} alt="Receipt preview" className="preview-image" />
          </div>
        )}

        {isUploading && (
          <div className="progress-container">
            <div className="progress-bar">
              <div
                className="progress-fill"
                style={{ width: `${uploadProgress}%` }}
              />
            </div>
            <span className="progress-text">Uploading... {uploadProgress}%</span>
          </div>
        )}

        {error && (
          <div className="error-message" role="alert">
            {error}
          </div>
        )}

        <div className="button-group">
          <Button
            variant="primary"
            onClick={handleUpload}
            isLoading={isUploading}
            disabled={!selectedFile || isUploading}
          >
            Upload Receipt
          </Button>
          {selectedFile && !isUploading && (
            <Button variant="secondary" onClick={handleClear}>
              Clear
            </Button>
          )}
        </div>
      </div>

      <style>{`
        .receipt-upload {
          max-width: 600px;
          margin: 0 auto;
        }

        .upload-container {
          background: white;
          border-radius: 8px;
          padding: 2rem;
          box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        }

        .upload-area {
          margin-bottom: 1.5rem;
        }

        .file-input {
          display: none;
        }

        .file-label {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          padding: 3rem 2rem;
          border: 2px dashed #cbd5e0;
          border-radius: 8px;
          cursor: pointer;
          transition: all 0.2s;
          background: #f7fafc;
        }

        .file-label:hover {
          border-color: #4299e1;
          background: #ebf8ff;
        }

        .upload-icon {
          font-size: 3rem;
          margin-bottom: 1rem;
        }

        .file-hint {
          font-size: 0.875rem;
          color: #718096;
          margin-top: 0.5rem;
        }

        .preview-container {
          margin-bottom: 1.5rem;
        }

        .preview-image {
          max-width: 100%;
          max-height: 300px;
          border-radius: 8px;
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }

        .progress-container {
          margin-bottom: 1.5rem;
        }

        .progress-bar {
          height: 8px;
          background: #e2e8f0;
          border-radius: 4px;
          overflow: hidden;
          margin-bottom: 0.5rem;
        }

        .progress-fill {
          height: 100%;
          background: #4299e1;
          transition: width 0.3s ease;
        }

        .progress-text {
          font-size: 0.875rem;
          color: #4a5568;
        }

        .error-message {
          padding: 1rem;
          background: #fed7d7;
          color: #c53030;
          border-radius: 4px;
          margin-bottom: 1.5rem;
        }

        .button-group {
          display: flex;
          gap: 1rem;
        }
      `}</style>
    </div>
  );
};

export default ReceiptUpload;
