using System.Globalization;
using System.Text;
using AgenticShopper.Core.Models;

namespace AgenticShopper.Core.Services;

/// <summary>
/// Service for exporting data to CSV format
/// </summary>
public class CsvExportService
{
    /// <summary>
    /// Export shopping list to CSV format
    /// </summary>
    public byte[] ExportShoppingList(ShoppingList list, IEnumerable<ShoppingListItem> items)
    {
        var csv = new StringBuilder();
        
        // Add header
        csv.AppendLine("Shopping List Export");
        csv.AppendLine($"List Name,{EscapeCsvValue(list.Name)}");
        csv.AppendLine($"Created Date,{list.CreatedDate:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine($"Status,{list.Status}");
        csv.AppendLine();
        
        // Add items header
        csv.AppendLine("Product Name,Quantity,Urgency,Is Purchased,Added Date,Source");
        
        // Add items
        foreach (var item in items.OrderBy(i => i.IsPurchased).ThenBy(i => i.Urgency))
        {
            csv.AppendLine($"{EscapeCsvValue(item.Product?.Name ?? "Unknown")}," +
                          $"{item.Quantity}," +
                          $"{item.Urgency}," +
                          $"{(item.IsPurchased ? "Yes" : "No")}," +
                          $"{item.AddedDate:yyyy-MM-dd HH:mm:ss}," +
                          $"{item.Source}");
        }
        
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
    
    /// <summary>
    /// Export receipt to CSV format
    /// </summary>
    public byte[] ExportReceipt(Receipt receipt, IEnumerable<Purchase> purchases)
    {
        var csv = new StringBuilder();
        
        // Add header
        csv.AppendLine("Receipt Export");
        csv.AppendLine($"Store Name,{EscapeCsvValue(receipt.StoreName)}");
        csv.AppendLine($"Purchase Date,{receipt.PurchaseDate:yyyy-MM-dd}");
        csv.AppendLine($"Total Amount,{receipt.TotalAmount:C}");
        csv.AppendLine($"Status,{receipt.Status}");
        csv.AppendLine($"Confidence Score,{receipt.ConfidenceScore:P1}");
        csv.AppendLine();
        
        // Add purchases header
        csv.AppendLine("Product Name,Quantity,Unit Price,Total Price,Category");
        
        // Add purchases
        foreach (var purchase in purchases.OrderBy(p => p.Product?.Category?.Name).ThenBy(p => p.Product?.Name))
        {
            csv.AppendLine($"{EscapeCsvValue(purchase.Product?.Name ?? "Unknown")}," +
                          $"{purchase.Quantity}," +
                          $"{purchase.UnitPrice:C}," +
                          $"{purchase.TotalPrice:C}," +
                          $"{EscapeCsvValue(purchase.Product?.Category?.Name ?? "Uncategorized")}");
        }
        
        // Add total
        csv.AppendLine();
        csv.AppendLine($"Total,,,,{purchases.Sum(p => p.TotalPrice):C}");
        
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
    
    /// <summary>
    /// Export multiple receipts for a date range
    /// </summary>
    public byte[] ExportReceipts(IEnumerable<Receipt> receipts, DateTime? startDate = null, DateTime? endDate = null)
    {
        var csv = new StringBuilder();
        
        // Add header
        csv.AppendLine("Receipts Export");
        csv.AppendLine($"Export Date,{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        if (startDate.HasValue)
            csv.AppendLine($"Start Date,{startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue)
            csv.AppendLine($"End Date,{endDate.Value:yyyy-MM-dd}");
        csv.AppendLine();
        
        // Add receipts header
        csv.AppendLine("Receipt ID,Store Name,Purchase Date,Total Amount,Status,Confidence Score,Verified Date");
        
        // Add receipts
        foreach (var receipt in receipts.OrderByDescending(r => r.PurchaseDate))
        {
            csv.AppendLine($"{receipt.Id}," +
                          $"{EscapeCsvValue(receipt.StoreName)}," +
                          $"{receipt.PurchaseDate:yyyy-MM-dd}," +
                          $"{receipt.TotalAmount:C}," +
                          $"{receipt.Status}," +
                          $"{receipt.ConfidenceScore:P1}," +
                          $"{(receipt.VerifiedDate.HasValue ? receipt.VerifiedDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}");
        }
        
        // Add summary
        csv.AppendLine();
        csv.AppendLine($"Total Receipts,{receipts.Count()}");
        csv.AppendLine($"Total Amount,{receipts.Sum(r => r.TotalAmount):C}");
        
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
    
    /// <summary>
    /// Export products with purchase history
    /// </summary>
    public byte[] ExportProducts(IEnumerable<Product> products)
    {
        var csv = new StringBuilder();
        
        // Add header
        csv.AppendLine("Products Export");
        csv.AppendLine($"Export Date,{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine();
        
        // Add products header
        csv.AppendLine("Product Name,Category,Average Price,Frequency,Last Purchased,Notes,Tags");
        
        // Add products
        foreach (var product in products.OrderBy(p => p.Category?.Name).ThenBy(p => p.Name))
        {
            csv.AppendLine($"{EscapeCsvValue(product.Name)}," +
                          $"{EscapeCsvValue(product.Category?.Name ?? "Uncategorized")}," +
                          $"{product.AveragePrice:C}," +
                          $"{product.Frequency}," +
                          $"{(product.LastPurchased.HasValue ? product.LastPurchased.Value.ToString("yyyy-MM-dd") : "")}," +
                          $"{EscapeCsvValue(product.Notes ?? "")}," +
                          $"{EscapeCsvValue(product.Tags ?? "")}");
        }
        
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
    
    /// <summary>
    /// Export budget summary
    /// </summary>
    public byte[] ExportBudgets(IEnumerable<Budget> budgets)
    {
        var csv = new StringBuilder();
        
        // Add header
        csv.AppendLine("Budget Export");
        csv.AppendLine($"Export Date,{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine();
        
        // Add budgets header
        csv.AppendLine("Category,Budget Amount,Current Spent,Remaining,Period,Start Date,End Date,Alert Threshold,Percentage Used");
        
        // Add budgets
        foreach (var budget in budgets.OrderBy(b => b.Category?.Name))
        {
            var remaining = budget.Amount - budget.CurrentSpent;
            var percentageUsed = budget.Amount > 0 ? (budget.CurrentSpent / budget.Amount) * 100 : 0;
            
            csv.AppendLine($"{EscapeCsvValue(budget.Category?.Name ?? "Unknown")}," +
                          $"{budget.Amount:C}," +
                          $"{budget.CurrentSpent:C}," +
                          $"{remaining:C}," +
                          $"{budget.Period}," +
                          $"{budget.StartDate:yyyy-MM-dd}," +
                          $"{budget.EndDate:yyyy-MM-dd}," +
                          $"{budget.AlertThreshold:P0}," +
                          $"{percentageUsed:F1}%");
        }
        
        // Add summary
        csv.AppendLine();
        csv.AppendLine($"Total Budget,{budgets.Sum(b => b.Amount):C}");
        csv.AppendLine($"Total Spent,{budgets.Sum(b => b.CurrentSpent):C}");
        csv.AppendLine($"Total Remaining,{budgets.Sum(b => b.Amount - b.CurrentSpent):C}");
        
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
    
    /// <summary>
    /// Escape CSV value to handle commas, quotes, and newlines
    /// </summary>
    private string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        
        // If value contains comma, quote, or newline, wrap in quotes and escape existing quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        
        return value;
    }
}
