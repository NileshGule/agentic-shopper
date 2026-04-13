using AgenticShopper.Agents.Receipt.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace AgenticShopper.Agents.Receipt.Services;

/// <summary>
/// Offline OCR service that shells out to the tesseract CLI.
/// Supports image files (JPEG, PNG, TIFF, BMP) and PDF files (via pdftoppm conversion).
/// Requires: apk add tesseract-ocr tesseract-ocr-data-eng poppler-utils
/// </summary>
public class TesseractOcrService : IOcrService
{
    private readonly ILogger<TesseractOcrService> _logger;

    private static readonly string[] SupportedImageExtensions =
        [".png", ".jpg", ".jpeg", ".tiff", ".tif", ".bmp", ".webp"];

    public string ProviderName => "Tesseract (Offline CLI)";

    public TesseractOcrService(ILogger<TesseractOcrService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OcrResult> ExtractReceiptDataAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ocr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            _logger.LogInformation("Processing receipt '{FileName}' with Tesseract CLI", fileName);

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var isPdf = ext == ".pdf";

            // Save the uploaded stream to a temporary file
            var inputPath = Path.Combine(tempDir, $"input{ext}");
            await using (var fs = File.Create(inputPath))
            {
                await imageStream.CopyToAsync(fs, cancellationToken);
            }

            string rawText;

            if (isPdf)
            {
                _logger.LogInformation("Input is a PDF – converting to images with pdftoppm");
                rawText = await ProcessPdfAsync(inputPath, tempDir, cancellationToken);
            }
            else
            {
                // Ensure the extension is one Tesseract understands
                if (!SupportedImageExtensions.Contains(ext))
                {
                    // Rename to .png as a safe fallback
                    var safePath = Path.Combine(tempDir, "input.png");
                    File.Move(inputPath, safePath);
                    inputPath = safePath;
                }

                rawText = await RunTesseractAsync(inputPath, tempDir, "output", cancellationToken);
            }

            if (rawText == null)
            {
                return new OcrResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Tesseract produced no output",
                    ProviderName = ProviderName
                };
            }

            _logger.LogInformation("Tesseract OCR extracted {Len} characters", rawText.Length);
            _logger.LogDebug("Raw OCR text:\n{RawText}", rawText);

            // Parse the raw text into structured receipt data
            var storeName = ExtractStoreName(rawText);
            var purchaseDate = ExtractPurchaseDate(rawText);
            var lineItems = ExtractLineItems(rawText);
            var totalAmount = ExtractTotalAmount(rawText) ?? lineItems.Sum(li => li.TotalPrice);

            var confidence = EstimateConfidence(storeName, purchaseDate, lineItems, totalAmount);

            _logger.LogInformation(
                "Parsed receipt: Store='{Store}', Date={Date}, Total={Total}, Items={Items}, Confidence={Conf:P1}",
                storeName, purchaseDate, totalAmount, lineItems.Count, confidence);

            return new OcrResult
            {
                IsSuccess = true,
                StoreName = storeName,
                PurchaseDate = purchaseDate,
                TotalAmount = totalAmount,
                ConfidenceScore = confidence,
                RawText = rawText,
                ProviderName = ProviderName,
                LineItems = lineItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing receipt with Tesseract CLI");
            return new OcrResult
            {
                IsSuccess = false,
                ErrorMessage = $"Tesseract OCR error: {ex.Message}",
                ProviderName = ProviderName
            };
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
        }
    }

    // ─── PDF Conversion ─────────────────────────────────────────

    /// <summary>
    /// Converts a PDF to page images using pdftoppm, then runs Tesseract on each page
    /// and concatenates the OCR results.
    /// </summary>
    private async Task<string?> ProcessPdfAsync(
        string pdfPath, string tempDir, CancellationToken cancellationToken)
    {
        // Convert PDF pages to PNG images using pdftoppm (from poppler-utils)
        // Output files will be named page-1.png, page-2.png, etc.
        var pagePrefix = Path.Combine(tempDir, "page");
        var psi = new ProcessStartInfo
        {
            FileName = "pdftoppm",
            Arguments = $"-png -r 300 \"{pdfPath}\" \"{pagePrefix}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(psi)!)
        {
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError("pdftoppm failed (exit {Code}): {Err}", process.ExitCode, stderr);
                return null;
            }
        }

        // Find all generated page images and sort them by name
        var pageImages = Directory.GetFiles(tempDir, "page-*.png")
            .OrderBy(f => f)
            .ToList();

        if (pageImages.Count == 0)
        {
            _logger.LogError("pdftoppm produced no page images from PDF");
            return null;
        }

        _logger.LogInformation("PDF converted to {Count} page image(s)", pageImages.Count);

        // Run Tesseract on each page and concatenate results
        var sb = new StringBuilder();
        for (int i = 0; i < pageImages.Count; i++)
        {
            var pageText = await RunTesseractAsync(
                pageImages[i], tempDir, $"ocr-page-{i}", cancellationToken);

            if (!string.IsNullOrWhiteSpace(pageText))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(pageText);
            }
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    // ─── Tesseract Execution ────────────────────────────────────

    /// <summary>
    /// Runs tesseract on a single image file and returns the extracted text.
    /// </summary>
    private async Task<string?> RunTesseractAsync(
        string imagePath, string tempDir, string outputName, CancellationToken cancellationToken)
    {
        var outputBase = Path.Combine(tempDir, outputName);
        var psi = new ProcessStartInfo
        {
            FileName = "tesseract",
            Arguments = $"\"{imagePath}\" \"{outputBase}\" --oem 3 --psm 6",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            _logger.LogError("Tesseract CLI exited with code {Code}: {Err}", process.ExitCode, stderr);
            return null;
        }

        var outputFile = outputBase + ".txt";
        if (!File.Exists(outputFile))
        {
            _logger.LogWarning("Tesseract produced no output file for {Image}", Path.GetFileName(imagePath));
            return null;
        }

        return await File.ReadAllTextAsync(outputFile, cancellationToken);
    }

    public Task<bool> IsAvailableAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(3000);
            var available = process is not null && process.ExitCode == 0;
            _logger.LogDebug("Tesseract CLI available: {Available}", available);
            return Task.FromResult(available);
        }
        catch
        {
            _logger.LogDebug("Tesseract CLI not found on PATH");
            return Task.FromResult(false);
        }
    }

    // ─── Receipt Parsing ────────────────────────────────────────────

    private string ExtractStoreName(string rawText)
    {
        var lines = GetNonEmptyLines(rawText);

        // Known Australian supermarket names
        var knownStores = new[]
        {
            "Coles", "Woolworths", "ALDI", "IGA", "Costco", "Harris Farm",
            "Foodland", "Spar", "FoodWorks", "BigW", "Big W",
            "Kmart", "Target", "Bunnings"
        };

        foreach (var store in knownStores)
        {
            if (lines.Any(l => l.Contains(store, StringComparison.OrdinalIgnoreCase)))
                return store;
        }

        // Fallback: first non-trivial line
        return lines.FirstOrDefault(l => l.Trim().Length > 2)?.Trim() ?? "Unknown Store";
    }

    private DateTime? ExtractPurchaseDate(string rawText)
    {
        // Common date patterns on Australian receipts
        var patterns = new[]
        {
            @"(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{4})",   // DD/MM/YYYY
            @"(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{2})\b",  // DD/MM/YY
            @"(\d{1,2})\s+(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\w*\s+(\d{4})", // 2 Jan 2025
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(rawText, pattern, RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            if (DateTime.TryParse(match.Value, out var date))
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);

            // Try DD/MM/YYYY (Australian format)
            var parts = Regex.Split(match.Value, @"[/\-.]");
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out var day) &&
                int.TryParse(parts[1], out var month) &&
                int.TryParse(parts[2], out var year))
            {
                if (year < 100) year += 2000;
                if (day <= 31 && month <= 12 && year >= 2000 && year <= 2100)
                {
                    try { return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc); } catch { }
                }
            }
        }

        _logger.LogWarning("Could not extract purchase date from OCR text");
        return null;
    }

    private decimal? ExtractTotalAmount(string rawText)
    {
        var lines = GetNonEmptyLines(rawText);
        var patterns = new[]
        {
            @"(?:^|\s)TOTAL\s+\$?\s*(\d+[.,]\d{2})",
            @"(?:^|\s)TOTAL\s*[:=]?\s*\$?\s*(\d+[.,]\d{2})",
            @"AMOUNT\s+DUE\s*\$?\s*(\d+[.,]\d{2})",
            @"(?:EFTPOS|VISA|CREDIT|DEBIT)\s+\$?\s*(\d+[.,]\d{2})",
        };

        // Search from bottom up (total is usually near the end)
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(lines[i], pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var val = match.Groups[1].Value.Replace(",", ".");
                    if (decimal.TryParse(val, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var total))
                        return total;
                }
            }
        }

        return null;
    }

    private List<OcrLineItem> ExtractLineItems(string rawText)
    {
        var items = new List<OcrLineItem>();
        var lines = GetNonEmptyLines(rawText);

        var skipWords = new[]
        {
            "total", "subtotal", "sub-total", "gst", "tax", "change", "eftpos",
            "visa", "mastercard", "credit", "debit", "cash", "balance",
            "receipt", "abn", "acn", "phone", "tel", "thank", "loyalty",
            "reward", "points", "member", "date", "time", "store",
            "flybuys", "everyday", "saving"
        };

        // Pattern: product name followed by a price
        var priceRx = new Regex(@"^(.+?)\s+\$?\s*(\d+[.,]\d{2})\s*$", RegexOptions.Compiled);
        // Pattern: qty x product price
        var qtyRx = new Regex(@"^(\d+)\s*[xX@]\s*(.+?)\s+\$?\s*(\d+[.,]\d{2})\s*$", RegexOptions.Compiled);

        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.Length < 3) continue;
            var lower = t.ToLower();
            if (skipWords.Any(w => lower.Contains(w))) continue;

            // Try qty pattern first
            var qm = qtyRx.Match(t);
            if (qm.Success)
            {
                var qty = ParseDec(qm.Groups[1].Value);
                var name = Clean(qm.Groups[2].Value);
                var tot = ParseDec(qm.Groups[3].Value);
                if (!string.IsNullOrWhiteSpace(name) && tot > 0)
                {
                    items.Add(new OcrLineItem
                    {
                        ProductName = name,
                        Quantity = qty,
                        UnitPrice = qty > 0 ? Math.Round(tot / qty, 2) : tot,
                        TotalPrice = tot,
                        Confidence = 0.7,
                        RawText = t
                    });
                    continue;
                }
            }

            // Simple: "PRODUCT   PRICE"
            var sm = priceRx.Match(t);
            if (sm.Success)
            {
                var name = Clean(sm.Groups[1].Value);
                var price = ParseDec(sm.Groups[2].Value);
                if (!string.IsNullOrWhiteSpace(name) && price > 0 && name.Length > 1)
                {
                    items.Add(new OcrLineItem
                    {
                        ProductName = name,
                        Quantity = 1,
                        UnitPrice = price,
                        TotalPrice = price,
                        Confidence = 0.65,
                        RawText = t
                    });
                }
            }
        }

        _logger.LogInformation("Extracted {Count} line items from receipt", items.Count);
        return items;
    }

    private static double EstimateConfidence(string? storeName, DateTime? date,
        List<OcrLineItem> items, decimal? total)
    {
        double score = 0.3; // base
        if (!string.IsNullOrEmpty(storeName) && storeName != "Unknown Store") score += 0.2;
        if (date.HasValue) score += 0.15;
        if (items.Count > 0) score += 0.2;
        if (total.HasValue && total > 0) score += 0.15;
        return Math.Min(score, 1.0);
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static string[] GetNonEmptyLines(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

    private static string Clean(string name)
    {
        name = name.Trim().TrimEnd('$', ' ', '\t');
        name = Regex.Replace(name, @"^[\d\.\-\*]+\s*", "");
        return name.Trim();
    }

    private static decimal ParseDec(string v)
    {
        var c = v.Replace(",", ".").Trim();
        return decimal.TryParse(c, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 0;
    }
}
