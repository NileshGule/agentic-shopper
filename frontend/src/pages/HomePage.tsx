import { Link } from "react-router-dom";

export default function HomePage() {
  return (
    <div className="home-page">
      <h1>Agentic Shopper</h1>
      <p>Smart Shopping Pattern Analyzer & Recommender</p>
      
      <nav className="home-nav">
        <Link to="/receipts" className="nav-card">
          <h2>📸 Receipts</h2>
          <p>Upload and manage shopping receipts</p>
        </Link>
        
        <Link to="/products" className="nav-card">
          <h2>🛒 Products</h2>
          <p>View and categorize products</p>
        </Link>
        
        <Link to="/shopping-lists" className="nav-card">
          <h2>📝 Shopping Lists</h2>
          <p>Generate and manage shopping lists</p>
        </Link>
        
        <Link to="/analytics" className="nav-card">
          <h2>📊 Analytics</h2>
          <p>View spending insights and budgets</p>
        </Link>
      </nav>
    </div>
  );
}
