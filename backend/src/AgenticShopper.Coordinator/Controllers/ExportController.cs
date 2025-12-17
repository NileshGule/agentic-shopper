using AgenticShopper.Core.Services;
using AgenticShopper.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgenticShopper.Coordinator.Controllers;

/// <summary>
/// Controller for exporting data to CSV format (FR-049)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly CsvExportService _csvExportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        ApplicationDbContext context,
        CsvExportService csvExportService,
        ILogger<ExportController> logger)
    {
        _context = context;
        _csvExportService = csvExportService;
        _logger = logger;
    }

    /// <summary>
    /// Export a specific shopping list to CSV
    /// </summary>
    [HttpGet("shopping-list/{listId}")]
    public async Task<IActionResult> ExportShoppingList(Guid listId)
    {
        var familyId = GetFamilyId();
        
        var list = await _context.ShoppingLists
            .Include(l => l.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(l => l.Id == listId && l.FamilyId == familyId);

        if (list == null)
        {
            return NotFound(new { message = "Shopping list not found" });
        }

        var csvData = _csvExportService.ExportShoppingList(list, list.Items);
        var fileName = $"shopping-list-{list.Name.Replace(" ", "-")}-{DateTime.UtcNow:yyyyMMdd}.csv";

        _logger.LogInformation("Exported shopping list {ListId} for family {FamilyId}", listId, familyId);

        return File(csvData, "text/csv", fileName);
    }

    /// <summary>
    /// Export a specific receipt to CSV
    /// </summary>
    [HttpGet("receipt/{receiptId}")]
    public async Task<IActionResult> ExportReceipt(Guid receiptId)
    {
        var familyId = GetFamilyId();
        
        var receipt = await _context.Receipts
            .Include(r => r.Purchases)
                .ThenInclude(p => p.Product)
                    .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(r => r.Id == receiptId && r.FamilyId == familyId);

        if (receipt == null)
        {
            return NotFound(new { message = "Receipt not found" });
        }

        var csvData = _csvExportService.ExportReceipt(receipt, receipt.Purchases);
        var fileName = $"receipt-{receipt.StoreName.Replace(" ", "-")}-{receipt.PurchaseDate:yyyyMMdd}.csv";

        _logger.LogInformation("Exported receipt {ReceiptId} for family {FamilyId}", receiptId, familyId);

        return File(csvData, "text/csv", fileName);
    }

    /// <summary>
    /// Export all receipts for a date range to CSV
    /// </summary>
    [HttpGet("receipts")]
    public async Task<IActionResult> ExportReceipts([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var familyId = GetFamilyId();
        
        var query = _context.Receipts
            .Where(r => r.FamilyId == familyId);

        if (startDate.HasValue)
        {
            query = query.Where(r => r.PurchaseDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.PurchaseDate <= endDate.Value);
        }

        var receipts = await query.ToListAsync();

        if (!receipts.Any())
        {
            return NotFound(new { message = "No receipts found for the specified date range" });
        }

        var csvData = _csvExportService.ExportReceipts(receipts, startDate, endDate);
        var dateRange = startDate.HasValue && endDate.HasValue 
            ? $"{startDate.Value:yyyyMMdd}-{endDate.Value:yyyyMMdd}"
            : DateTime.UtcNow.ToString("yyyyMMdd");
        var fileName = $"receipts-{dateRange}.csv";

        _logger.LogInformation("Exported {Count} receipts for family {FamilyId}", receipts.Count, familyId);

        return File(csvData, "text/csv", fileName);
    }

    /// <summary>
    /// Export all products to CSV
    /// </summary>
    [HttpGet("products")]
    public async Task<IActionResult> ExportProducts([FromQuery] Guid? categoryId)
    {
        var familyId = GetFamilyId();
        
        // Products are family-scoped through CreatedBy → UserProfile → Family relationship
        var query = _context.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query.ToListAsync();

        if (!products.Any())
        {
            return NotFound(new { message = "No products found" });
        }

        var csvData = _csvExportService.ExportProducts(products);
        var fileName = categoryId.HasValue
            ? $"products-category-{categoryId.Value}-{DateTime.UtcNow:yyyyMMdd}.csv"
            : $"products-all-{DateTime.UtcNow:yyyyMMdd}.csv";

        _logger.LogInformation("Exported {Count} products for family {FamilyId}", products.Count, familyId);

        return File(csvData, "text/csv", fileName);
    }

    /// <summary>
    /// Export all budgets to CSV
    /// </summary>
    [HttpGet("budgets")]
    public async Task<IActionResult> ExportBudgets()
    {
        var familyId = GetFamilyId();
        
        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.FamilyId == familyId)
            .ToListAsync();

        if (!budgets.Any())
        {
            return NotFound(new { message = "No budgets found" });
        }

        var csvData = _csvExportService.ExportBudgets(budgets);
        var fileName = $"budgets-{DateTime.UtcNow:yyyyMMdd}.csv";

        _logger.LogInformation("Exported {Count} budgets for family {FamilyId}", budgets.Count, familyId);

        return File(csvData, "text/csv", fileName);
    }

    /// <summary>
    /// Export all shopping lists to CSV (batch export)
    /// </summary>
    [HttpGet("shopping-lists")]
    public async Task<IActionResult> ExportAllShoppingLists([FromQuery] bool activeOnly = true)
    {
        var familyId = GetFamilyId();
        
        var query = _context.ShoppingLists
            .Include(l => l.Items)
                .ThenInclude(i => i.Product)
            .Where(l => l.FamilyId == familyId);

        if (activeOnly)
        {
            query = query.Where(l => l.Status == Core.Models.ListStatus.Active || l.Status == Core.Models.ListStatus.InProgress);
        }

        var lists = await query.ToListAsync();

        if (!lists.Any())
        {
            return NotFound(new { message = "No shopping lists found" });
        }

        // Combine all lists into a single CSV
        var allCsvData = new List<byte[]>();
        foreach (var list in lists)
        {
            var csvData = _csvExportService.ExportShoppingList(list, list.Items);
            allCsvData.Add(csvData);
        }

        // Merge CSV files with separators
        var combinedCsv = string.Join("\n\n========================================\n\n", 
            allCsvData.Select(data => System.Text.Encoding.UTF8.GetString(data)));
        var finalCsvData = System.Text.Encoding.UTF8.GetBytes(combinedCsv);

        var fileName = $"shopping-lists-{(activeOnly ? "active" : "all")}-{DateTime.UtcNow:yyyyMMdd}.csv";

        _logger.LogInformation("Exported {Count} shopping lists for family {FamilyId}", lists.Count, familyId);

        return File(finalCsvData, "text/csv", fileName);
    }

    private Guid GetFamilyId()
    {
        var familyIdClaim = User.FindFirst("FamilyId")?.Value;
        if (string.IsNullOrEmpty(familyIdClaim) || !Guid.TryParse(familyIdClaim, out var familyId))
        {
            throw new UnauthorizedAccessException("Family ID not found in token claims");
        }
        return familyId;
    }
}
