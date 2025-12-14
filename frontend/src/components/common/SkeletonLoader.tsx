import React from 'react';
import './SkeletonLoader.css';

export interface SkeletonLoaderProps {
  variant?: 'text' | 'rectangular' | 'circular' | 'list-item' | 'card' | 'table-row';
  width?: string | number;
  height?: string | number;
  count?: number;
  className?: string;
}

/**
 * Skeleton loader component for showing loading states
 * Provides visual placeholders while content loads
 */
export const SkeletonLoader: React.FC<SkeletonLoaderProps> = ({
  variant = 'text',
  width,
  height,
  count = 1,
  className = ''
}) => {
  const getSkeletonClass = () => {
    switch (variant) {
      case 'text':
        return 'skeleton-text';
      case 'rectangular':
        return 'skeleton-rect';
      case 'circular':
        return 'skeleton-circle';
      case 'list-item':
        return 'skeleton-list-item';
      case 'card':
        return 'skeleton-card';
      case 'table-row':
        return 'skeleton-table-row';
      default:
        return 'skeleton-text';
    }
  };

  const skeletonClass = `skeleton ${getSkeletonClass()} ${className}`;
  
  const style: React.CSSProperties = {};
  if (width) style.width = typeof width === 'number' ? `${width}px` : width;
  if (height) style.height = typeof height === 'number' ? `${height}px` : height;

  // Render multiple skeleton elements if count > 1
  if (count > 1) {
    return (
      <div className="skeleton-group">
        {Array.from({ length: count }).map((_, index) => (
          <div key={index} className={skeletonClass} style={style} />
        ))}
      </div>
    );
  }

  return <div className={skeletonClass} style={style} />;
};

/**
 * Pre-configured skeleton for shopping list items
 */
export const ShoppingListSkeleton: React.FC = () => {
  return (
    <div className="skeleton-shopping-list">
      <div className="skeleton-list-header">
        <SkeletonLoader variant="text" width="40%" height={24} />
        <SkeletonLoader variant="circular" width={32} height={32} />
      </div>
      <div className="skeleton-list-items">
        <SkeletonLoader variant="list-item" count={5} />
      </div>
    </div>
  );
};

/**
 * Pre-configured skeleton for receipt cards
 */
export const ReceiptCardSkeleton: React.FC = () => {
  return (
    <div className="skeleton-receipt-card">
      <SkeletonLoader variant="rectangular" width="100%" height={120} />
      <div className="skeleton-card-content">
        <SkeletonLoader variant="text" width="60%" height={18} />
        <SkeletonLoader variant="text" width="40%" height={14} />
        <SkeletonLoader variant="text" width="30%" height={14} />
      </div>
    </div>
  );
};

/**
 * Pre-configured skeleton for analytics charts
 */
export const ChartSkeleton: React.FC = () => {
  return (
    <div className="skeleton-chart">
      <SkeletonLoader variant="text" width="50%" height={20} />
      <SkeletonLoader variant="rectangular" width="100%" height={300} />
      <div className="skeleton-chart-legend">
        <SkeletonLoader variant="text" width="25%" height={14} count={3} />
      </div>
    </div>
  );
};

/**
 * Pre-configured skeleton for data table rows
 */
export const TableSkeleton: React.FC<{ rows?: number; columns?: number }> = ({ 
  rows = 5, 
  columns = 4 
}) => {
  return (
    <div className="skeleton-table">
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={rowIndex} className="skeleton-table-row">
          {Array.from({ length: columns }).map((_, colIndex) => (
            <SkeletonLoader 
              key={colIndex} 
              variant="text" 
              width={`${80 + Math.random() * 20}%`} 
              height={16} 
            />
          ))}
        </div>
      ))}
    </div>
  );
};

export default SkeletonLoader;
