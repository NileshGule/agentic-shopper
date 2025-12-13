import React, { useState, useEffect } from 'react';
import { PurchaseFrequency } from '../../services/api/productApi';
import './FrequencyAssignment.css';

export interface FrequencyData {
  frequency: PurchaseFrequency;
  isManual: boolean;
  lastPurchased?: string;
  purchaseCount: number;
  averageDaysBetween?: number;
  nextExpectedDate?: string;
  isPaused: boolean;
}

export interface FrequencyAssignmentProps {
  productId: string;
  frequencyData: FrequencyData;
  onFrequencyChange: (productId: string, frequency: PurchaseFrequency, isManual: boolean) => Promise<void>;
  onTogglePause: (productId: string, isPaused: boolean) => Promise<void>;
  onRecalculate?: (productId: string) => Promise<void>;
  onMarkPurchased?: (productId: string, purchaseDate: Date) => Promise<void>;
  disabled?: boolean;
}

const FREQUENCY_OPTIONS: Array<{ value: PurchaseFrequency; label: string; description: string; icon: string }> = [
  { value: PurchaseFrequency.Weekly, label: 'Weekly', description: '~7 days', icon: '📅' },
  { value: PurchaseFrequency.Fortnightly, label: 'Fortnightly', description: '~14 days', icon: '🗓️' },
  { value: PurchaseFrequency.Monthly, label: 'Monthly', description: '~30 days', icon: '📆' },
  { value: PurchaseFrequency.Quarterly, label: 'Quarterly', description: '~90 days', icon: '🗒️' },
  { value: PurchaseFrequency.Annually, label: 'Annually', description: '~365 days', icon: '🎂' },
  { value: PurchaseFrequency.Occasional, label: 'Occasional', description: 'As needed', icon: '🌟' },
];

/**
 * Component for managing product purchase frequency
 * Features:
 * - Display current frequency with visual indicators
 * - Manual frequency override
 * - Pause/resume frequency tracking (vacation mode)
 * - Recalculate frequency based on purchase history
 * - Show next expected purchase date
 */
export const FrequencyAssignment: React.FC<FrequencyAssignmentProps> = ({
  productId,
  frequencyData,
  onFrequencyChange,
  onTogglePause,
  onRecalculate,
  onMarkPurchased,
  disabled = false
}) => {
  const [selectedFrequency, setSelectedFrequency] = useState<PurchaseFrequency>(
    frequencyData.frequency
  );
  const [isLoading, setIsLoading] = useState(false);
  const [isRecalculating, setIsRecalculating] = useState(false);
  const [showMarkPurchased, setShowMarkPurchased] = useState(false);
  const [purchaseDate, setPurchaseDate] = useState(new Date().toISOString().split('T')[0]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setSelectedFrequency(frequencyData.frequency);
  }, [frequencyData.frequency]);

  const handleFrequencyChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newFrequency = e.target.value as PurchaseFrequency;
    
    setSelectedFrequency(newFrequency);
    setError(null);
    setIsLoading(true);

    try {
      await onFrequencyChange(productId, newFrequency, true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update frequency');
      setSelectedFrequency(frequencyData.frequency);
    } finally {
      setIsLoading(false);
    }
  };

  const handleTogglePause = async () => {
    setError(null);
    setIsLoading(true);

    try {
      await onTogglePause(productId, !frequencyData.isPaused);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to toggle pause');
    } finally {
      setIsLoading(false);
    }
  };

  const handleRecalculate = async () => {
    if (!onRecalculate) return;

    setError(null);
    setIsRecalculating(true);

    try {
      await onRecalculate(productId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to recalculate frequency');
    } finally {
      setIsRecalculating(false);
    }
  };
  const handleMarkPurchased = async () => {
    if (!onMarkPurchased) return;

    setError(null);
    setIsLoading(true);

    try {
      await onMarkPurchased(productId, new Date(purchaseDate));
      setShowMarkPurchased(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to mark as purchased');
    } finally {
      setIsLoading(false);
    }
  };
  const formatDate = (dateString?: string): string => {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-AU', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric' 
    });
  };

  const getFrequencyIcon = (frequency: PurchaseFrequency): string => {
    const option = FREQUENCY_OPTIONS.find(o => o.value === frequency);
    return option?.icon || '❓';
  };

  const canRecalculate = frequencyData.purchaseCount >= 3 && !frequencyData.isPaused;

  return (
    <div className={`frequency-assignment ${frequencyData.isPaused ? 'paused' : ''}`}>
      <div className="frequency-header">
        <div className="frequency-title">
          <label htmlFor={`frequency-${productId}`} className="frequency-label">
            Purchase Frequency
          </label>
          {frequencyData.isPaused && (
            <span className="paused-badge">⏸️ Paused</span>
          )}
        </div>
        {selectedFrequency !== 'Unknown' && (
          <span className={`frequency-badge ${frequencyData.isManual ? 'manual' : 'auto'}`}>
            {frequencyData.isManual ? '👤 Manual' : '🤖 Auto'}
          </span>
        )}
      </div>

      <div className="frequency-controls">
        <div className="frequency-select-container">
          <span className="frequency-icon">{getFrequencyIcon(selectedFrequency)}</span>
          <select
            id={`frequency-${productId}`}
            className="frequency-select"
            value={selectedFrequency}
            onChange={handleFrequencyChange}
            disabled={disabled || isLoading || frequencyData.isPaused}
          >
            {selectedFrequency === 'Unknown' && (
              <option value="Unknown">Unknown (insufficient data)</option>
            )}
            {FREQUENCY_OPTIONS.map(option => (
              <option key={option.value} value={option.value}>
                {option.label} - {option.description}
              </option>
            ))}
          </select>
        </div>

        <div className="frequency-actions">
          {onRecalculate && canRecalculate && (
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={handleRecalculate}
              disabled={disabled || isRecalculating}
              title="Recalculate frequency based on purchase history"
            >
              {isRecalculating ? '🔄' : '🔁'} Recalculate
            </button>
          )}
          
          <button
            type="button"
            className={`btn btn-sm ${frequencyData.isPaused ? 'btn-success' : 'btn-warning'}`}
            onClick={handleTogglePause}
            disabled={disabled || isLoading}
            title={frequencyData.isPaused ? 'Resume tracking' : 'Pause tracking (vacation mode)'}
          >
            {frequencyData.isPaused ? '▶️ Resume' : '⏸️ Pause'}
          </button>
        </div>
      </div>

      {onMarkPurchased && (
        <div className="mark-purchased-section">
          <button
            onClick={() => setShowMarkPurchased(!showMarkPurchased)}
            disabled={disabled || isLoading}
            className="mark-purchased-toggle"
          >
            🛒 Mark as Purchased Externally
          </button>

          {showMarkPurchased && (
            <div className="purchase-date-picker">
              <input
                type="date"
                value={purchaseDate}
                onChange={(e) => setPurchaseDate(e.target.value)}
                max={new Date().toISOString().split('T')[0]}
                disabled={disabled || isLoading}
              />
              <button
                onClick={handleMarkPurchased}
                disabled={disabled || isLoading}
                className="submit-purchase"
              >
                {isLoading ? 'Submitting...' : 'Submit'}
              </button>
              <button
                onClick={() => setShowMarkPurchased(false)}
                disabled={disabled || isLoading}
                className="cancel-purchase"
              >
                Cancel
              </button>
            </div>
          )}
        </div>
      )}

      {error && (
        <div className="error-message">
          <span className="error-icon">⚠️</span>
          {error}
        </div>
      )}

      <div className="frequency-info">
        <div className="info-grid">
          <div className="info-item">
            <span className="info-label">Purchase Count:</span>
            <span className="info-value">
              {frequencyData.purchaseCount}
              {frequencyData.purchaseCount < 3 && (
                <span className="info-hint"> (need 3+ for auto-calculation)</span>
              )}
            </span>
          </div>

          {frequencyData.lastPurchased && (
            <div className="info-item">
              <span className="info-label">Last Purchased:</span>
              <span className="info-value">{formatDate(frequencyData.lastPurchased)}</span>
            </div>
          )}

          {frequencyData.averageDaysBetween && (
            <div className="info-item">
              <span className="info-label">Avg. Days Between:</span>
              <span className="info-value">{Math.round(frequencyData.averageDaysBetween)} days</span>
            </div>
          )}

          {frequencyData.nextExpectedDate && !frequencyData.isPaused && (
            <div className="info-item">
              <span className="info-label">Next Expected:</span>
              <span className="info-value next-date">
                {formatDate(frequencyData.nextExpectedDate)}
              </span>
            </div>
          )}
        </div>

        {frequencyData.isPaused && (
          <div className="paused-notice">
            <span className="notice-icon">ℹ️</span>
            <span>Frequency tracking is paused. This product will not appear in generated shopping lists.</span>
          </div>
        )}
      </div>
    </div>
  );
};
