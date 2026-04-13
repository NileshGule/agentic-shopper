import React, { useState, useEffect } from 'react';
import {
  budgetApi,
  BudgetDto,
  BudgetAlert,
  CreateBudgetDto,
  UpdateBudgetDto,
  BudgetPeriod
} from '../../services/api/budgetApi';
import { TableSkeleton } from '../common/SkeletonLoader';
import './BudgetTracker.css';

export interface BudgetTrackerProps {
  familyId: string;
  categories: Array<{ id: string; name: string }>;
  onAlertChange?: (alertCount: number) => void;
}

/**
 * BudgetTracker component for managing budgets (T143, T150, T151, T152)
 */
export const BudgetTracker: React.FC<BudgetTrackerProps> = ({
  familyId,
  categories,
  onAlertChange
}) => {
  const [budgets, setBudgets] = useState<BudgetDto[]>([]);
  const [alerts, setAlerts] = useState<BudgetAlert[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [editingBudget, setEditingBudget] = useState<BudgetDto | null>(null);

  // Form state
  const [formData, setFormData] = useState<CreateBudgetDto>({
    familyId,
    categoryId: '',
    amount: 0,
    period: 'Monthly' as BudgetPeriod,
    alertThreshold: 0.9
  });

  useEffect(() => {
    loadBudgets();
    loadAlerts();
  }, [familyId]);

  useEffect(() => {
    if (onAlertChange) {
      onAlertChange(alerts.length);
    }
  }, [alerts, onAlertChange]);

  const loadBudgets = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await budgetApi.getFamilyBudgets(familyId);
      setBudgets(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load budgets');
      console.error('Error loading budgets:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadAlerts = async () => {
    try {
      const data = await budgetApi.getActiveAlerts(familyId);
      setAlerts(data);
    } catch (err) {
      console.error('Error loading alerts:', err);
    }
  };

  const handleCreateBudget = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      await budgetApi.createBudget(formData);
      setShowCreateForm(false);
      resetForm();
      await loadBudgets();
      await loadAlerts();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create budget');
    }
  };

  const handleUpdateBudget = async (budgetId: string, updates: UpdateBudgetDto) => {
    try {
      await budgetApi.updateBudget(budgetId, updates);
      setEditingBudget(null);
      await loadBudgets();
      await loadAlerts();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update budget');
    }
  };

  const handleDeleteBudget = async (budgetId: string) => {
    if (!confirm('Are you sure you want to delete this budget?')) {
      return;
    }

    try {
      await budgetApi.deleteBudget(budgetId);
      await loadBudgets();
      await loadAlerts();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete budget');
    }
  };

  const handleDismissAlert = (alertId: string) => {
    setAlerts(prev => prev.filter(alert => alert.budgetId !== alertId));
  };

  const resetForm = () => {
    setFormData({
      familyId,
      categoryId: '',
      amount: 0,
      period: 'Monthly' as BudgetPeriod,
      alertThreshold: 0.9
    });
  };

  const getStatusColor = (status: string): string => {
    switch (status) {
      case 'On Track':
        return '#2ecc71';
      case 'Warning':
        return '#f39c12';
      case 'Over Budget':
        return '#e74c3c';
      default:
        return '#95a5a6';
    }
  };

  const getStatusIcon = (status: string): string => {
    switch (status) {
      case 'On Track':
        return '✓';
      case 'Warning':
        return '⚠';
      case 'Over Budget':
        return '✗';
      default:
        return '?';
    }
  };

  if (loading) {
    return (
      <div className="budget-tracker">
        <div className="tracker-header">
          <h2>Budget Tracker</h2>
          <button className="create-button" disabled style={{ opacity: 0.5 }}>
            + New Budget
          </button>
        </div>
        <TableSkeleton rows={6} columns={5} />
      </div>
    );
  }

  return (
    <div className="budget-tracker">
      <div className="tracker-header">
        <h2>Budget Tracker</h2>
        <button
          onClick={() => setShowCreateForm(!showCreateForm)}
          className="create-button"
        >
          {showCreateForm ? 'Cancel' : '+ New Budget'}
        </button>
      </div>

      {error && (
        <div className="error-banner">
          <span>{error}</span>
          <button onClick={() => setError(null)}>×</button>
        </div>
      )}

      {/* T151: Budget alert notifications */}
      {alerts.length > 0 && (
        <div className="alerts-section">
          <h3>⚠ Active Alerts ({alerts.length})</h3>
          {alerts.map((alert) => (
            <div
              key={alert.budgetId}
              className={`alert-card ${alert.isOverBudget ? 'over-budget' : 'warning'}`}
            >
              <div className="alert-content">
                <h4>{alert.categoryName}</h4>
                <p>{alert.message}</p>
                <div className="alert-details">
                  <span>Spent: ${alert.currentSpent.toFixed(2)} / ${alert.amount.toFixed(2)}</span>
                  <span>Period: {new Date(alert.periodStart).toLocaleDateString()} - {new Date(alert.periodEnd).toLocaleDateString()}</span>
                </div>
              </div>
              <button
                className="dismiss-button"
                onClick={() => handleDismissAlert(alert.budgetId)}
                title="Dismiss"
              >
                ×
              </button>
            </div>
          ))}
        </div>
      )}

      {/* T150: Budget creation and editing */}
      {showCreateForm && (
        <div className="create-form">
          <h3>Create New Budget</h3>
          <form onSubmit={handleCreateBudget}>
            <div className="form-group">
              <label>Category</label>
              <select
                value={formData.categoryId}
                onChange={(e) => setFormData({ ...formData, categoryId: e.target.value })}
                required
              >
                <option value="">Select category...</option>
                {categories.map((cat) => (
                  <option key={cat.id} value={cat.id}>
                    {cat.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Amount ($)</label>
              <input
                type="number"
                min="0"
                step="0.01"
                value={formData.amount || ''}
                onChange={(e) => setFormData({ ...formData, amount: parseFloat(e.target.value) || 0 })}
                required
              />
            </div>

            <div className="form-group">
              <label>Period</label>
              <select
                value={formData.period}
                onChange={(e) => setFormData({ ...formData, period: e.target.value as BudgetPeriod })}
                required
              >
                <option value="Weekly">Weekly</option>
                <option value="Monthly">Monthly</option>
              </select>
            </div>

            <div className="form-group">
              <label>
                Alert Threshold ({((formData.alertThreshold ?? 0) * 100).toFixed(0)}%)
                <span className="hint">Alert when spending reaches this percentage</span>
              </label>
              <input
                type="range"
                min="50"
                max="100"
                step="5"
                value={(formData.alertThreshold ?? 0.9) * 100}
                onChange={(e) => setFormData({ ...formData, alertThreshold: parseInt(e.target.value) / 100 })}
              />
            </div>

            <div className="form-actions">
              <button type="submit" className="submit-button">
                Create Budget
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowCreateForm(false);
                  resetForm();
                }}
                className="cancel-button"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Budget list with progress bars (T152) */}
      <div className="budgets-list">
        {budgets.length === 0 ? (
          <div className="empty-state">
            <p>No budgets configured yet</p>
            <button onClick={() => setShowCreateForm(true)} className="create-button">
              Create Your First Budget
            </button>
          </div>
        ) : (
          budgets.map((budget) => (
            <div key={budget.id} className="budget-card">
              <div className="budget-header">
                <div className="budget-title">
                  <h3>{budget.categoryName}</h3>
                  <span className="period-badge">{budget.period}</span>
                  <span
                    className="status-badge"
                    style={{ backgroundColor: getStatusColor(budget.status) }}
                  >
                    {getStatusIcon(budget.status)} {budget.status}
                  </span>
                </div>
                <div className="budget-actions">
                  <button
                    onClick={() => setEditingBudget(budget)}
                    className="edit-button"
                    title="Edit"
                  >
                    ✎
                  </button>
                  <button
                    onClick={() => handleDeleteBudget(budget.id)}
                    className="delete-button"
                    title="Delete"
                  >
                    🗑
                  </button>
                </div>
              </div>

              <div className="budget-amounts">
                <div className="amount-item">
                  <span className="label">Budget</span>
                  <span className="value">${budget.amount.toFixed(2)}</span>
                </div>
                <div className="amount-item">
                  <span className="label">Spent</span>
                  <span className="value spent">${budget.currentSpent.toFixed(2)}</span>
                </div>
                <div className="amount-item">
                  <span className="label">Remaining</span>
                  <span className="value">${budget.remaining.toFixed(2)}</span>
                </div>
              </div>

              {/* T152: Budget progress bar with threshold indicator */}
              <div className="progress-container">
                <div className="progress-bar-wrapper">
                  <div
                    className="progress-bar"
                    style={{
                      width: `${Math.min(budget.percentageUsed, 100)}%`,
                      backgroundColor: getStatusColor(budget.status)
                    }}
                  />
                  {/* Threshold indicator line */}
                  <div
                    className="threshold-marker"
                    style={{ left: `${budget.alertThreshold * 100}%` }}
                    title={`Alert threshold: ${(budget.alertThreshold * 100).toFixed(0)}%`}
                  />
                </div>
                <span className="progress-label">
                  {budget.percentageUsed.toFixed(1)}%
                </span>
              </div>

              <div className="budget-period">
                Period: {new Date(budget.startDate).toLocaleDateString()} - {new Date(budget.endDate).toLocaleDateString()}
              </div>

              {/* Inline edit form */}
              {editingBudget?.id === budget.id && (
                <div className="edit-form">
                  <div className="form-row">
                    <label>
                      Amount:
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        defaultValue={budget.amount}
                        id={`amount-${budget.id}`}
                      />
                    </label>
                    <label>
                      Threshold:
                      <input
                        type="range"
                        min="50"
                        max="100"
                        step="5"
                        defaultValue={budget.alertThreshold * 100}
                        id={`threshold-${budget.id}`}
                      />
                      <span>{(budget.alertThreshold * 100).toFixed(0)}%</span>
                    </label>
                  </div>
                  <div className="form-actions">
                    <button
                      onClick={() => {
                        const amount = parseFloat(
                          (document.getElementById(`amount-${budget.id}`) as HTMLInputElement)
                            ?.value || '0'
                        );
                        const threshold =
                          parseInt(
                            (document.getElementById(`threshold-${budget.id}`) as HTMLInputElement)
                              ?.value || '90'
                          ) / 100;
                        handleUpdateBudget(budget.id, { amount, alertThreshold: threshold });
                      }}
                      className="save-button"
                    >
                      Save
                    </button>
                    <button
                      onClick={() => setEditingBudget(null)}
                      className="cancel-button"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
};
