using AgenticShopper.Agents.Receipt.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AgenticShopper.Agents.Receipt.Services;

/// <summary>
/// OCR service using PaddleOCR for local development and fallback
/// </summary>
public class PaddleOcrService : IOcrService
{
    private readonly ILogger<PaddleOcrService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string? _paddleOcrPath;

    public string ProviderName => "PaddleOCR";

    public PaddleOcrService(
        ILogger<PaddleOcrService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        _paddleOcrPath = _configuration["PaddleOCR:ExecutablePath"];
    }

    public async Task<OcrResult> ExtractReceiptDataAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing receipt '{FileName}' with PaddleOCR", fileName);

            // TODO: Implement actual PaddleOCR integration
            // For now, return mock data for development
            _logger.LogWarning("PaddleOCR integration not yet implemented, returning mock data");

            // Simulate OCR processing delay
            await Task.Delay(500, cancellationToken);

            return new OcrResult
            {
                IsSuccess = true,
                StoreName = "Mock Store (PaddleOCR)",
                PurchaseDate = DateTime.UtcNow.AddDays(-2),
                TotalAmount = 78.45m,
                ConfidenceScore = 0.82,
                RawText = "Mock OCR text from PaddleOCR\nStore Name\nProduct 1 $15.99\nProduct 2 $32.23\nProduct 3 $30.23\nTotal: $78.45",
                ProviderName = ProviderName,
                LineItems = new List<OcrLineItem>
                {
                    new OcrLineItem
                    {
                        ProductName = "Mock Product 1",
                        Quantity = 1,
                        UnitPrice = 15.99m,
                        TotalPrice = 15.99m,
                        Confidence = 0.80,
                        RawText = "Product 1 $15.99"
                    },
                    new OcrLineItem
                    {
                        ProductName = "Mock Product 2",
                        Quantity = 1,
                        UnitPrice = 32.23m,
                        TotalPrice = 32.23m,
                        Confidence = 0.85,
                        RawText = "Product 2 $32.23"
                    },
                    new OcrLineItem
                    {
                        ProductName = "Mock Product 3",
                        Quantity = 1,
                        UnitPrice = 30.23m,
                        TotalPrice = 30.23m,
                        Confidence = 0.81,
                        RawText = "Product 3 $30.23"
                    }
                }
            };

            /*
            // ACTUAL IMPLEMENTATION (for future use):
            
            // Save stream to temporary file
            var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
            try
            {
                using (var fileStream = File.Create(tempFile))
                {
                    await imageStream.CopyToAsync(fileStream, cancellationToken);
                }

                // Run PaddleOCR executable
                var paddleExe = _paddleOcrPath ?? "paddleocr";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = paddleExe,
                        Arguments = $"--image_file \"{tempFile}\" --use_angle_cls true --use_gpu false",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    throw new Exception($"PaddleOCR failed with exit code {process.ExitCode}: {error}");
                }

                // Parse PaddleOCR output
                return ParsePaddleOcrOutput(output);
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing receipt with PaddleOCR");
            return new OcrResult
            {
                IsSuccess = false,
                ErrorMessage = $"PaddleOCR error: {ex.Message}",
                ProviderName = ProviderName
            };
        }
    }

    public Task<bool> IsAvailableAsync()
    {
        // PaddleOCR is always available as fallback (using mock data)
        // TODO: In production, check if PaddleOCR executable exists
        _logger.LogDebug("PaddleOCR is available (mock mode)");
        return Task.FromResult(true);
    }

    /*
    private OcrResult ParsePaddleOcrOutput(string output)
    {
        // Parse PaddleOCR JSON output format
        // This is a simplified parser - actual implementation would be more robust
        
        var lines = new List<string>();
        var confidenceScores = new List<double>();

        // Extract text lines and confidence scores from PaddleOCR output
        var lineMatches = Regex.Matches(output, @"\[""([^""]+)"", ([\d.]+)\]");
        foreach (Match match in lineMatches)
        {
            lines.Add(match.Groups[1].Value);
            confidenceScores.Add(double.Parse(match.Groups[2].Value));
        }

        var rawText = string.Join("\n", lines);
        var avgConfidence = confidenceScores.Any() ? confidenceScores.Average() : 0.0;

        // Extract receipt fields using regex patterns
        var storeName = ExtractStoreName(lines);
        var purchaseDate = ExtractDate(lines);
        var totalAmount = ExtractTotal(lines);
        var lineItems = ExtractLineItems(lines, confidenceScores);

        return new OcrResult
        {
            IsSuccess = true,
            StoreName = storeName,
            PurchaseDate = purchaseDate,
            TotalAmount = totalAmount,
            ConfidenceScore = avgConfidence,
            RawText = rawText,
            ProviderName = ProviderName,
            LineItems = lineItems
        };
    }

    private string? ExtractStoreName(List<string> lines)
    {
        // Look for store name in first few lines
        // Common patterns: all caps, followed by address
        return lines.FirstOrDefault(l => l.Length > 3 && l == l.ToUpper());
    }

    private DateTime? ExtractDate(List<string> lines)
    {
        // Look for date patterns
        var datePattern = @"(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})";
        foreach (var line in lines)
        {
            var match = Regex.Match(line, datePattern);
            if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var date))
            {
                return date;
            }
        }
        return null;
    }

    private decimal? ExtractTotal(List<string> lines)
    {
        // Look for total amount (usually near end, with keywords)
        var totalPattern = @"total[:\s]*\$?([\d,]+\.?\d{0,2})";
        foreach (var line in lines.Reverse<string>())
        {
            var match = Regex.Match(line, totalPattern, RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var total))
            {
                return total;
            }
        }
        return null;
    }

    private List<OcrLineItem> ExtractLineItems(List<string> lines, List<double> confidences)
    {
        var items = new List<OcrLineItem>();
        var itemPattern = @"^([^$\d]+)\s+\$?([\d,]+\.?\d{0,2})";

        for (int i = 0; i < lines.Count; i++)
        {
            var match = Regex.Match(lines[i], itemPattern);
            if (match.Success)
            {
                var productName = match.Groups[1].Value.Trim();
                if (decimal.TryParse(match.Groups[2].Value.Replace(",", ""), out var price))
                {
                    items.Add(new OcrLineItem
                    {
                        ProductName = productName,
                        Quantity = 1,
                        UnitPrice = price,
                        TotalPrice = price,
                        Confidence = i < confidences.Count ? confidences[i] : 0.5,
                        RawText = lines[i]
                    });
                }
            }
        }

        return items;
    }
    */
}
