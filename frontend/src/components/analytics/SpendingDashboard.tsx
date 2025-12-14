import React, { useState, useEffect } from 'react';
import { budgetApi, SpendingAnalyticsResponse, DataPoint } from '../../services/api/budgetApi';
import { LineChart, BarChart, PieChart } from './Charts';
import { ChartSkeleton } from '../common/SkeletonLoader';
import './SpendingDashboard.css';

export interface SpendingDashboardProps {
  familyId: string;
}

type TrendType = 'weekly' | 'monthly' | 'quarterly';

/**
 * SpendingDashboard component for visualizing spending analytics (T142, T147, T148, T149)
 */
export const SpendingDashboard: React.FC<SpendingDashboardProps> = ({ familyId }) => {
  const [analytics, setAnalytics] = useState<SpendingAnalyticsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [trendType, setTrendType] = useState<TrendType>('weekly');
  const [dateRange, setDateRange] = useState({
    startDate: new Date(new Date().setMonth(new Date().getMonth() - 3)).toISOString().split('T')[0],
    endDate: new Date().toISOString().split('T')[0]
  });

  useEffect(() => {
    loadAnalytics();
  }, [familyId, trendType, dateRange]);

  const loadAnalytics = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const data = await budgetApi.getSpendingAnalytics(familyId, {
        startDate: dateRange.startDate,
        endDate: dateRange.endDate,
        trendType
      });
      
      setAnalytics(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load analytics');
      console.error('Error loading analytics:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleTrendTypeChange = (type: TrendType) => {
    setTrendType(type);
  };

  const handleDateRangeChange = (field: 'startDate' | 'endDate', value: string) => {
    setDateRange(prev => ({
      ...prev,
      [field]: value
    }));
  };

  if (loading) {
    return (
      <div className="spending-dashboard">
        <div className="dashboard-header">
          <h2>Spending Analytics</h2>
          <div className="controls" style={{ opacity: 0.5 }}>
            <div className="trend-selector">
              <button className="active">Weekly</button>
              <button>Monthly</button>
              <button>Quarterly</button>
            </div>
          </div>
        </div>
        <div className="charts-grid">
          <ChartSkeleton />
          <ChartSkeleton />
          <ChartSkeleton />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="spending-dashboard error">
        <p className="error-message">Error: {error}</p>
        <button onClick={loadAnalytics} className="retry-button">
          Retry
        </button>
      </div>
    );
  }

  if (!analytics) {
    return (
      <div className="spending-dashboard empty">
        <p>No analytics data available</p>
      </div>
    );
  }

  const categoryLabels = Object.keys(analytics.categoryBreakdown);
  const categoryData = Object.values(analytics.categoryBreakdown);
  
  const storeLabels = Object.keys(analytics.storeBreakdown);
  const storeData = Object.values(analytics.storeBreakdown);

  const trendLabels = analytics.trendData.map((dp: DataPoint) => dp.label);
  const trendValues = analytics.trendData.map((dp: DataPoint) => dp.value);

  return (
    <div className="spending-dashboard">
      <div className="dashboard-header">
        <h2>Spending Analytics</h2>
        <div className="controls">
          <div className="date-range">
            <label>
              From:
              <input
                type="date"
                value={dateRange.startDate}
                onChange={(e) => handleDateRangeChange('startDate', e.target.value)}
                max={dateRange.endDate}
              />
            </label>
            <label>
              To:
              <input
                type="date"
                value={dateRange.endDate}
                onChange={(e) => handleDateRangeChange('endDate', e.target.value)}
                min={dateRange.startDate}
                max={new Date().toISOString().split('T')[0]}
              />
            </label>
          </div>
        </div>
      </div>

      <div className="summary-cards">
        <div className="summary-card">
          <h3>Total Spent</h3>
          <p className="amount">${analytics.totalSpent.toFixed(2)}</p>
        </div>
        <div className="summary-card">
          <h3>Transactions</h3>
          <p className="count">{analytics.transactionCount}</p>
        </div>
        <div className="summary-card">
          <h3>Avg per Transaction</h3>
          <p className="amount">${analytics.averagePerTransaction.toFixed(2)}</p>
        </div>
      </div>

      {/* T147: Spending trend visualizations */}
      <div className="chart-section">
        <div className="chart-header">
          <h3>Spending Trends</h3>
          <div className="trend-selector">
            <button
              className={trendType === 'weekly' ? 'active' : ''}
              onClick={() => handleTrendTypeChange('weekly')}
            >
              Weekly
            </button>
            <button
              className={trendType === 'monthly' ? 'active' : ''}
              onClick={() => handleTrendTypeChange('monthly')}
            >
              Monthly
            </button>
            <button
              className={trendType === 'quarterly' ? 'active' : ''}
              onClick={() => handleTrendTypeChange('quarterly')}
            >
              Quarterly
            </button>
          </div>
        </div>
        <div className="chart-container">
          {trendLabels.length > 0 ? (
            <LineChart
              labels={trendLabels}
              data={trendValues}
              label="Spending"
              color="rgb(75, 192, 192)"
              fillColor="rgba(75, 192, 192, 0.2)"
            />
          ) : (
            <p className="no-data">No trend data available for selected period</p>
          )}
        </div>
      </div>

      <div className="charts-row">
        {/* T148: Category spending breakdown */}
        <div className="chart-section half-width">
          <h3>Category Breakdown</h3>
          <div className="chart-container">
            {categoryLabels.length > 0 ? (
              <PieChart labels={categoryLabels} data={categoryData} />
            ) : (
              <p className="no-data">No category data available</p>
            )}
          </div>
        </div>

        {/* T149: Store spending distribution */}
        <div className="chart-section half-width">
          <h3>Store Distribution</h3>
          <div className="chart-container">
            {storeLabels.length > 0 ? (
              <BarChart
                labels={storeLabels}
                data={storeData}
                label="Total Spent"
                color="rgb(54, 162, 235)"
              />
            ) : (
              <p className="no-data">No store data available</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
