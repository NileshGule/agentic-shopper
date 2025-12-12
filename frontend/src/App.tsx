import { BrowserRouter, Routes, Route } from "react-router-dom";
import Header from "./components/common/Header";
import HomePage from "./pages/HomePage";
import ReceiptsPage from "./pages/ReceiptsPage";
import ProductsPage from "./pages/ProductsPage";
import ShoppingListsPage from "./pages/ShoppingListsPage";
import AnalyticsPage from "./pages/AnalyticsPage";
import "./App.css";

function App() {
  return (
    <BrowserRouter>
      <div className="app">
        <Header />
        <main className="main-content">
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/receipts" element={<ReceiptsPage />} />
            <Route path="/products" element={<ProductsPage />} />
            <Route path="/shopping-lists" element={<ShoppingListsPage />} />
            <Route path="/analytics" element={<AnalyticsPage />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}

export default App;
