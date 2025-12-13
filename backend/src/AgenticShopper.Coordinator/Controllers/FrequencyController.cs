using AgenticShopper.Agents.Frequency;
using AgenticShopper.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AgenticShopper.Coordinator.Controllers;

/// <summary>
/// REST API controller for purchase frequency operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FrequencyController : ControllerBase
{
    private readonly FrequencyAgent _frequencyAgent;
    private readonly ILogger<FrequencyController> _logger;

    public FrequencyController(
        FrequencyAgent frequencyAgent,
        ILogger<FrequencyController> logger)
    {
        _frequencyAgent = frequencyAgent;
        _logger = logger;
    }

    /// <summary>
    /// Calculate purchase frequency for a single product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="forceRecalculate">Force recalculation even if paused</param>
    [HttpPost("calculate/{productId}")]
    public async Task<ActionResult<FrequencyCalculationResponse>> CalculateFrequency(
        Guid productId,
        [FromQuery] bool forceRecalculate = false)
    {
        try
        {
            _logger.LogInformation("Calculating frequency for product {ProductId}", productId);

            var request = new FrequencyCalculationRequest
            {
                ProductId = productId,
                ForceRecalculate = forceRecalculate
            };

            var result = await _frequencyAgent.ExecuteAsync<FrequencyCalculationRequest, FrequencyCalculationResponse>(
                request, CancellationToken.None);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }

                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating frequency");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Calculate frequency for multiple products in batch
    /// </summary>
    [HttpPost("calculate/batch")]
    public async Task<ActionResult<List<FrequencyCalculationResponse>>> CalculateBatchFrequency(
        [FromBody] BatchFrequencyDto dto)
    {
        try
        {
            _logger.LogInformation("Batch calculating frequency for {Count} products", dto.ProductIds.Count);

            if (!dto.ProductIds.Any())
            {
                return BadRequest(new { error = "No product IDs provided" });
            }

            var request = new BatchFrequencyRequest
            {
                ProductIds = dto.ProductIds,
                ForceRecalculate = dto.ForceRecalculate
            };

            var results = await _frequencyAgent.CalculateBatchFrequencyAsync(request, CancellationToken.None);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch frequency calculation");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Pause frequency tracking for a product (FR-015)
    /// </summary>
    [HttpPost("{productId}/pause")]
    public async Task<ActionResult<FrequencyCalculationResponse>> PauseFrequency(Guid productId)
    {
        try
        {
            _logger.LogInformation("Pausing frequency for product {ProductId}", productId);

            var result = await _frequencyAgent.PauseFrequencyAsync(productId, CancellationToken.None);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }

                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing frequency");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Resume frequency tracking for a product
    /// </summary>
    [HttpPost("{productId}/resume")]
    public async Task<ActionResult<FrequencyCalculationResponse>> ResumeFrequency(Guid productId)
    {
        try
        {
            _logger.LogInformation("Resuming frequency for product {ProductId}", productId);

            var result = await _frequencyAgent.ResumeFrequencyAsync(productId, CancellationToken.None);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }

                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming frequency");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Manually override the frequency for a product
    /// </summary>
    [HttpPost("override")]
    public async Task<ActionResult<FrequencyCalculationResponse>> OverrideFrequency(
        [FromBody] OverrideFrequencyDto dto)
    {
        try
        {
            _logger.LogInformation("Manually overriding frequency for product {ProductId} to {Frequency}",
                dto.ProductId, dto.Frequency);

            if (!Enum.IsDefined(typeof(PurchaseFrequency), dto.Frequency))
            {
                return BadRequest(new { error = "Invalid frequency value" });
            }

            var result = await _frequencyAgent.ManualOverrideAsync(
                dto.ProductId,
                dto.Frequency,
                CancellationToken.None);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }

                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error overriding frequency");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Mark product as recently purchased outside system (FR-021)
    /// Updates last purchase date without creating receipt
    /// </summary>
    [HttpPost("{productId}/mark-purchased")]
    public async Task<ActionResult<FrequencyCalculationResponse>> MarkAsPurchased(
        Guid productId,
        [FromBody] MarkPurchasedDto dto)
    {
        try
        {
            _logger.LogInformation("Marking product {ProductId} as purchased on {Date}", 
                productId, dto.PurchaseDate);

            var result = await _frequencyAgent.MarkPurchasedExternallyAsync(
                productId, 
                dto.PurchaseDate, 
                CancellationToken.None);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }

                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking product as purchased");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all available frequency options
    /// </summary>
    [HttpGet("options")]
    public ActionResult<List<FrequencyOptionDto>> GetFrequencyOptions()
    {
        var options = Enum.GetValues<PurchaseFrequency>()
            .Select(f => new FrequencyOptionDto
            {
                Value = f,
                Label = f.ToString(),
                DaysEstimate = GetDaysEstimate(f)
            })
            .ToList();

        return Ok(options);
    }

    private int? GetDaysEstimate(PurchaseFrequency frequency)
    {
        return frequency switch
        {
            PurchaseFrequency.Weekly => 7,
            PurchaseFrequency.Fortnightly => 14,
            PurchaseFrequency.Monthly => 30,
            PurchaseFrequency.Quarterly => 90,
            PurchaseFrequency.Annually => 365,
            _ => null
        };
    }
}

/// <summary>
/// DTO for batch frequency calculation
/// </summary>
public class BatchFrequencyDto
{
    public List<Guid> ProductIds { get; set; } = new();
    public bool ForceRecalculate { get; set; }
}

/// <summary>
/// DTO for manual frequency override
/// </summary>
public class OverrideFrequencyDto
{
    public Guid ProductId { get; set; }
    public PurchaseFrequency Frequency { get; set; }
}

/// <summary>
/// DTO for frequency options
/// </summary>
public class FrequencyOptionDto
{
    public PurchaseFrequency Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public int? DaysEstimate { get; set; }
}

/// <summary>
/// DTO for marking product as purchased externally
/// </summary>
public class MarkPurchasedDto
{
    public DateTime PurchaseDate { get; set; }
}
