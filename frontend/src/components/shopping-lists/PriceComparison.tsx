import React, { useState, useEffect } from 'react';
import priceApi, {
  PriceComparisonResponse,
  ProductPriceComparisonDto,
} from '../../services/api/priceApi';
import './PriceComparison.css';

interface PriceComparisonProps {
  products: {
    productName: string;
    quantity: number;
  }[];
  onClose?: () => void;
}

const PriceComparison: React.FC<PriceComparisonProps> = ({
  products,
  onClose,
}) => {
  const [comparison, setComparison] = useState<PriceComparisonResponse | null>(
    null
  );
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (products.length > 0) {
      loadPriceComparison();
    }
  }, [products]);

  const loadPriceComparison = async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await priceApi.comparePricesForList(products);
      setComparison(result);
    } catch (err) {
      setError('Failed to load price comparison. Please try again.');
      console.error('Error loading price comparison:', err);
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount?: number): string => {
    if (amount === undefined || amount === null) return 'N/A';
    return `$${amount.toFixed(2)}`;
  };

  const getStoreColor = (store: string): string => {
    if (store === 'Coles') return '#e31837';
    if (store === 'Woolworths') return '#1b7340';
    return '#666';
  };

  if (loading) {
    return (
      <div className="price-comparison loading">
        <div className="loading-spinner">Loading price comparison...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="price-comparison error">
        <p className="error-message">{error}</p>
        <button onClick={loadPriceComparison} className="retry-button">
          Retry
        </button>
        {onClose && (
          <button onClick={onClose} className="close-button">
            Close
          </button>
        )}
      </div>
    );
  }

  if (!comparison) {
    return null;
  }

  return (
    <div className="price-comparison">
      <div className="price-comparison-header">
        <h2>Price Comparison</h2>
        {onClose && (
          <button onClick={onClose} className="close-button" aria-label="Close">
            ×
          </button>
        )}
      </div>

      {/* Savings Summary */}
      <div className="savings-summary">
        <div className="summary-card">
          <h3>Shopping Summary</h3>
          <div className="summary-grid">
            <div className="summary-item">
              <span className="label">Coles Total:</span>
              <span className="value coles-color">
                {formatCurrency(comparison.summary.colesTotal)}
              </span>
            </div>
            <div className="summary-item">
              <span className="label">Woolworths Total:</span>
              <span className="value woolworths-color">
                {formatCurrency(comparison.summary.woolworthsTotal)}
              </span>
            </div>
            <div className="summary-item highlight">
              <span className="label">Potential Savings:</span>
              <span className="value savings-color">
                {formatCurrency(comparison.summary.potentialSavings)}
              </span>
            </div>
            <div className="summary-item recommended">
              <span className="label">Recommended:</span>
              <span
                className="value"
                style={{ color: getStoreColor(comparison.summary.recommendedStore) }}
              >
                {comparison.summary.recommendedStore}
              </span>
            </div>
          </div>
        </div>

        {/* Split Strategy */}
        {comparison.summary.splitStrategy && (
          <div className="split-strategy">
            <h4>💡 Split Shopping Strategy</h4>
            <p className="split-savings">
              Save <strong>{formatCurrency(comparison.summary.splitStrategy.totalSavings)}</strong> by
              shopping at both stores!
            </p>
            <div className="split-lists">
              <div className="split-list coles">
                <h5>Buy at Coles ({comparison.summary.splitStrategy.colesPurchases.length} items)</h5>
                <ul>
                  {comparison.summary.splitStrategy.colesPurchases.map((item, idx) => (
                    <li key={idx}>{item}</li>
                  ))}
                </ul>
              </div>
              <div className="split-list woolworths">
                <h5>
                  Buy at Woolworths ({comparison.summary.splitStrategy.woolworthsPurchases.length}{' '}
                  items)
                </h5>
                <ul>
                  {comparison.summary.splitStrategy.woolworthsPurchases.map((item, idx) => (
                    <li key={idx}>{item}</li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Price Comparison Table */}
      <div className="price-table-container">
        <h3>Product-by-Product Comparison</h3>
        <table className="price-table">
          <thead>
            <tr>
              <th>Product</th>
              <th>Qty</th>
              <th className="coles-header">Coles</th>
              <th className="woolworths-header">Woolworths</th>
              <th>Best Price</th>
              <th>Savings</th>
            </tr>
          </thead>
          <tbody>
            {comparison.items.map((item, idx) => (
              <tr key={idx}>
                <td className="product-name">{item.productName}</td>
                <td className="quantity">{item.quantity}</td>
                <td className="coles-price">
                  <div className="price-cell">
                    {item.hasColesPromotion && (
                      <span className="original-price">
                        {formatCurrency(item.colesPrice)}
                      </span>
                    )}
                    <span className={item.hasColesPromotion ? 'sale-price' : ''}>
                      {formatCurrency(item.colesSalePrice || item.colesPrice)}
                    </span>
                    {item.hasColesPromotion && (
                      <span className="promo-badge">
                        🏷️{' '}
                        {item.colesPrice && item.colesSalePrice
                          ? Math.round(
                              ((item.colesPrice - item.colesSalePrice) /
                                item.colesPrice) *
                                100
                            )
                          : 0}
                        % OFF
                      </span>
                    )}
                  </div>
                </td>
                <td className="woolworths-price">
                  <div className="price-cell">
                    {item.hasWoolworthsPromotion && (
                      <span className="original-price">
                        {formatCurrency(item.woolworthsPrice)}
                      </span>
                    )}
                    <span className={item.hasWoolworthsPromotion ? 'sale-price' : ''}>
                      {formatCurrency(
                        item.woolworthsSalePrice || item.woolworthsPrice
                      )}
                    </span>
                    {item.hasWoolworthsPromotion && (
                      <span className="promo-badge">
                        🏷️{' '}
                        {item.woolworthsPrice && item.woolworthsSalePrice
                          ? Math.round(
                              ((item.woolworthsPrice - item.woolworthsSalePrice) /
                                item.woolworthsPrice) *
                                100
                            )
                          : 0}
                        % OFF
                      </span>
                    )}
                  </div>
                </td>
                <td className="best-price">
                  <span
                    className="store-badge"
                    style={{ backgroundColor: getStoreColor(item.bestStore) }}
                  >
                    {item.bestStore}
                  </span>
                  <span className="price">{formatCurrency(item.bestPrice)}</span>
                </td>
                <td className="savings">
                  {item.savings > 0 ? (
                    <span className="savings-amount">
                      {formatCurrency(item.savings)}
                    </span>
                  ) : (
                    <span className="no-savings">-</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="price-comparison-footer">
        <button onClick={loadPriceComparison} className="refresh-button">
          🔄 Refresh Prices
        </button>
      </div>
    </div>
  );
};

export default PriceComparison;
