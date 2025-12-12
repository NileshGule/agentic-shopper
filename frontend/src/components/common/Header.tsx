import { Link } from "react-router-dom";

export default function Header() {
  return (
    <header className="app-header">
      <div className="container">
        <Link to="/" className="logo">
          🛒 Agentic Shopper
        </Link>
        <nav className="main-nav">
          <Link to="/receipts">Receipts</Link>
          <Link to="/products">Products</Link>
          <Link to="/shopping-lists">Lists</Link>
          <Link to="/analytics">Analytics</Link>
        </nav>
      </div>
    </header>
  );
}
