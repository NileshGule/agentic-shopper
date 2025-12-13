import React, { useState, useEffect } from 'react';
import { SpendingDashboard } from '../components/analytics/SpendingDashboard';
import { BudgetTracker } from '../components/analytics/BudgetTracker';
import './AnalyticsPage.css';

interface Category {
  id: string;
  name: string;
}

/**
 * AnalyticsPage - Integrates SpendingDashboard and BudgetTracker (T146)
 */
export default function AnalyticsPage() {
  // TODO: Get familyId from auth context
  const [familyId] = useState('00000000-0000-0000-0000-000000000001');
  const [categories, setCategories] = useState<Category[]>([]);
  const [alertCount, setAlertCount] = useState(0);
  const [view, setView] = useState<'dashboard' | 'budgets' | 'both'>('both');

  useEffect(() => {
    // TODO: Load categories from API
    // For now, using hardcoded predefined categories
    setCategories([
      { id: '00000000-0000-0000-0000-000000000001', name: 'Groceries' },
      { id: '00000000-0000-0000-0000-000000000002', name: 'Dairy & Eggs' },
      { id: '00000000-0000-0000-0000-000000000003', name: 'Meat & Seafood' },
      { id: '00000000-0000-0000-0000-000000000004', name: 'Fruits & Vegetables' },
      { id: '00000000-0000-0000-0000-000000000005', name: 'Bakery' },
      { id: '00000000-0000-0000-0000-000000000006', name: 'Frozen Foods' },
      { id: '00000000-0000-0000-0000-000000000007', name: 'Pantry' },
      { id: '00000000-0000-0000-0000-000000000008', name: 'Beverages' },
      { id: '00000000-0000-0000-0000-000000000009', name: 'Snacks' },
      { id: '00000000-0000-0000-0000-000000000010', name: 'Health & Beauty' },
      { id: '00000000-0000-0000-0000-000000000011', name: 'Household' }
    ]);
  }, []);

  return (
    <div className="analytics-page">
      <div className="page-header">
        <h1>Analytics & Budgets</h1>
        {alertCount > 0 && (
          <div className="alert-badge">
            {alertCount} {alertCount === 1 ? 'Alert' : 'Alerts'}
          </div>
        )}
      </div>

      <div className="view-selector">
        <button
          className={view === 'both' ? 'active' : ''}
          onClick={() => setView('both')}
        >
          Overview
        </button>
        <button
          className={view === 'dashboard' ? 'active' : ''}
          onClick={() => setView('dashboard')}
        >
          Analytics
        </button>
        <button
          className={view === 'budgets' ? 'active' : ''}
          onClick={() => setView('budgets')}
        >
          Budgets
        </button>
      </div>

      <div className={`content-layout ${view}`}>
        {(view === 'dashboard' || view === 'both') && (
          <div className="dashboard-section">
            <SpendingDashboard familyId={familyId} />
          </div>
        )}

        {(view === 'budgets' || view === 'both') && (
          <div className="budgets-section">
            <BudgetTracker
              familyId={familyId}
              categories={categories}
              onAlertChange={setAlertCount}
            />
          </div>
        )}
      </div>
    </div>
  );
}
