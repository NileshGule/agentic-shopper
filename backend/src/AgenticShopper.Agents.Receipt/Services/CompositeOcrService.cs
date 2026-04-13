using AgenticShopper.Agents.Receipt.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Agents.Receipt.Services;

/// <summary>
/// Composite OCR service that tries Azure Document Intelligence first, falls back to PaddleOCR
/// In development mode with OCR:PreferredProvider=PaddleOCR, uses PaddleOCR first for cost savings
/// </summary>
public class CompositeOcrService : IOcrService
{
    private readonly ILogger<CompositeOcrService> _logger;
    private readonly IConfiguration _configuration;
    private readonly List<IOcrService> _ocrServices;

    public string ProviderName => "Composite (Configurable Priority)";

    public CompositeOcrService(
        ILogger<CompositeOcrService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Initialize OCR providers in priority order
        _ocrServices = new List<IOcrService>();

        var preferredProvider = _configuration["OCR:PreferredProvider"];

        // Add Azure Document Intelligence if configured
        var azureEndpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
        var azureKey = _configuration["AzureDocumentIntelligence:ApiKey"];

        if (!string.IsNullOrEmpty(azureEndpoint) && !string.IsNullOrEmpty(azureKey)
            && preferredProvider != "Tesseract")
        {
            _ocrServices.Add(new AzureDocumentIntelligenceOcrService(
                logger: LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AzureDocumentIntelligenceOcrService>(),
                configuration: _configuration));
            _logger.LogInformation("Azure Document Intelligence OCR provider configured");
        }

        // Always add Tesseract as the local / offline provider
        _ocrServices.Add(new TesseractOcrService(
            logger: LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<TesseractOcrService>()));
        _logger.LogInformation("Tesseract (offline) OCR provider configured");

        // If Tesseract is the preferred provider, move it to the front
        if (preferredProvider == "Tesseract")
        {
            _logger.LogInformation("Tesseract is the preferred OCR provider");
            var tesseract = _ocrServices.Last();
            _ocrServices.Remove(tesseract);
            _ocrServices.Insert(0, tesseract);
        }
    }

    public async Task<OcrResult> ExtractReceiptDataAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing receipt '{FileName}' with composite OCR service", fileName);

        foreach (var ocrService in _ocrServices)
        {
            try
            {
                // Check if service is available
                if (!await ocrService.IsAvailableAsync())
                {
                    _logger.LogWarning(
                        "OCR provider '{Provider}' is not available, trying next provider",
                        ocrService.ProviderName);
                    continue;
                }

                _logger.LogInformation("Attempting OCR with provider: {Provider}", ocrService.ProviderName);

                // Reset stream position before processing
                if (imageStream.CanSeek)
                {
                    imageStream.Position = 0;
                }

                var result = await ocrService.ExtractReceiptDataAsync(imageStream, fileName, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "OCR successful with provider '{Provider}', confidence: {Confidence:P}",
                        ocrService.ProviderName,
                        result.ConfidenceScore);
                    return result;
                }

                _logger.LogWarning(
                    "OCR provider '{Provider}' failed: {Error}",
                    ocrService.ProviderName,
                    result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "OCR provider '{Provider}' threw exception, trying next provider",
                    ocrService.ProviderName);
            }
        }

        // All providers failed
        _logger.LogError("All OCR providers failed for receipt '{FileName}'", fileName);
        return new OcrResult
        {
            IsSuccess = false,
            ErrorMessage = "All OCR providers failed to process the receipt",
            ProviderName = ProviderName
        };
    }

    public async Task<bool> IsAvailableAsync()
    {
        // Composite service is available if at least one provider is available
        foreach (var ocrService in _ocrServices)
        {
            if (await ocrService.IsAvailableAsync())
            {
                return true;
            }
        }

        return false;
    }
}
