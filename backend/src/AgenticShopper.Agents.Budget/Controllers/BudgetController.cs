using AgenticShopper.Agents.Budget.Services;
using AgenticShopper.Core.Models;
using AgenticShopper.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AgenticShopper.Agents.Budget.Controllers;

/// <summary>
/// DTO for creating a budget
/// </summary>
public class CreateBudgetDto
{
    public Guid FamilyId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public BudgetPeriod Period { get; set; }
    public decimal? AlertThreshold { get; set; }
}

/// <summary>
/// DTO for updating a budget
/// </summary>
public class UpdateBudgetDto
{
    public decimal? Amount { get; set; }
    public decimal? AlertThreshold { get; set; }
}

/// <summary>
/// DTO for budget response
/// </summary>
public class BudgetDto
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public BudgetPeriod Period { get; set; }
    public decimal CurrentSpent { get; set; }
    public decimal Remaining { get; set; }
    public decimal PercentageUsed { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AlertThreshold { get; set; }
    public DateTime? LastAlertSent { get; set; }
    public string Status { get; set; } = "On Track";
}

/// <summary>
/// DTO for spending analytics request
/// </summary>
public class AnalyticsRequestDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string TrendType { get; set; } = "monthly"; // weekly, monthly, quarterly
}

/// <summary>
/// Controller for budget tracking and spending analytics
/// </summary>
[ApiController]
[Route("api/v1/budgets")]
public class BudgetController : ControllerBase
{
    private readonly BudgetAgent _budgetAgent;
    private readonly BudgetRepository _budgetRepository;
    private readonly SpendingAnalyzer _spendingAnalyzer;
    private readonly BudgetAlertService _alertService;
    private readonly ILogger<BudgetController> _logger;

    public BudgetController(
        BudgetAgent budgetAgent,
        BudgetRepository budgetRepository,
        SpendingAnalyzer spendingAnalyzer,
        BudgetAlertService alertService,
        ILogger<BudgetController> logger)
    {
        _budgetAgent = budgetAgent;
        _budgetRepository = budgetRepository;
        _spendingAnalyzer = spendingAnalyzer;
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new budget
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBudget(
        [FromBody] CreateBudgetDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating budget for family {FamilyId}, category {CategoryId}",
                dto.FamilyId, dto.CategoryId);

            var request = new CreateBudgetRequest
            {
                FamilyId = dto.FamilyId,
                CategoryId = dto.CategoryId,
                Amount = dto.Amount,
                Period = dto.Period,
                AlertThreshold = dto.AlertThreshold
            };

            var result = await _budgetAgent.Execute<CreateBudgetRequest, Core.Models.Budget>(
                request, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            var budgetDto = MapToBudgetDto(result.Data);

            return CreatedAtAction(
                nameof(GetBudget),
                new { id = budgetDto.Id },
                budgetDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating budget");
            return StatusCode(500, new { error = "An error occurred while creating the budget" });
        }
    }

    /// <summary>
    /// Get a budget by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBudget(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var budget = await _budgetRepository.GetByIdAsync(id, cancellationToken);
            if (budget == null)
            {
                return NotFound(new { error = $"Budget with ID {id} not found" });
            }

            var budgetDto = MapToBudgetDto(budget);
            return Ok(budgetDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budget {BudgetId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the budget" });
        }
    }

    /// <summary>
    /// Get all active budgets for a family
    /// </summary>
    [HttpGet("family/{familyId}")]
    [ProducesResponseType(typeof(List<BudgetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFamilyBudgets(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        try
        {
            var budgets = await _budgetRepository.GetActiveBudgetsByFamilyAsync(
                familyId, cancellationToken);

            var budgetDtos = budgets.Select(MapToBudgetDto).ToList();

            return Ok(budgetDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budgets for family {FamilyId}", familyId);
            return StatusCode(500, new { error = "An error occurred while retrieving budgets" });
        }
    }

    /// <summary>
    /// Update a budget
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBudget(
        Guid id,
        [FromBody] UpdateBudgetDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new UpdateBudgetRequest
            {
                BudgetId = id,
                Amount = dto.Amount,
                AlertThreshold = dto.AlertThreshold
            };

            var result = await _budgetAgent.Execute<UpdateBudgetRequest, Core.Models.Budget>(
                request, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }
                return BadRequest(new { error = result.ErrorMessage });
            }

            var budgetDto = MapToBudgetDto(result.Data);
            return Ok(budgetDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating budget {BudgetId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the budget" });
        }
    }

    /// <summary>
    /// Delete a budget
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBudget(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _budgetRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { error = $"Budget with ID {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting budget {BudgetId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the budget" });
        }
    }

    /// <summary>
    /// Get budget status and alerts for a family
    /// </summary>
    [HttpGet("family/{familyId}/status")]
    [ProducesResponseType(typeof(BudgetStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBudgetStatus(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new BudgetStatusRequest { FamilyId = familyId };

            var result = await _budgetAgent.Execute<BudgetStatusRequest, BudgetStatusResponse>(
                request, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budget status for family {FamilyId}", familyId);
            return StatusCode(500, new { error = "An error occurred while retrieving budget status" });
        }
    }

    /// <summary>
    /// Get spending analytics for a family
    /// </summary>
    [HttpPost("family/{familyId}/analytics")]
    [ProducesResponseType(typeof(SpendingAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpendingAnalytics(
        Guid familyId,
        [FromBody] AnalyticsRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var startDate = dto.StartDate ?? DateTime.UtcNow.AddMonths(-3);
            var endDate = dto.EndDate ?? DateTime.UtcNow;

            var request = new SpendingAnalyticsRequest
            {
                FamilyId = familyId,
                StartDate = startDate,
                EndDate = endDate,
                TrendType = dto.TrendType
            };

            var result = await _budgetAgent.Execute<SpendingAnalyticsRequest, SpendingAnalyticsResponse>(
                request, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analytics for family {FamilyId}", familyId);
            return StatusCode(500, new { error = "An error occurred while retrieving analytics" });
        }
    }

    /// <summary>
    /// Get active alerts for a family
    /// </summary>
    [HttpGet("family/{familyId}/alerts")]
    [ProducesResponseType(typeof(List<BudgetAlert>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        try
        {
            var alerts = await _alertService.GetActiveAlertsAsync(familyId, cancellationToken);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts for family {FamilyId}", familyId);
            return StatusCode(500, new { error = "An error occurred while retrieving alerts" });
        }
    }

    /// <summary>
    /// Recalculate all budget spending from purchases
    /// </summary>
    [HttpPost("family/{familyId}/recalculate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecalculateBudgets(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _alertService.RecalculateAllBudgetsAsync(familyId, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating budgets for family {FamilyId}", familyId);
            return StatusCode(500, new { error = "An error occurred while recalculating budgets" });
        }
    }

    /// <summary>
    /// Helper method to map Budget entity to DTO
    /// </summary>
    private BudgetDto MapToBudgetDto(Core.Models.Budget budget)
    {
        var percentageUsed = budget.Amount > 0
            ? (budget.CurrentSpent / budget.Amount) * 100
            : 0;

        var status = "On Track";
        if (budget.CurrentSpent >= budget.Amount)
        {
            status = "Over Budget";
        }
        else if (percentageUsed >= budget.AlertThreshold * 100)
        {
            status = "Warning";
        }

        return new BudgetDto
        {
            Id = budget.Id,
            FamilyId = budget.FamilyId,
            CategoryId = budget.CategoryId,
            CategoryName = budget.Category?.Name ?? "Unknown",
            Amount = budget.Amount,
            Period = budget.Period,
            CurrentSpent = budget.CurrentSpent,
            Remaining = budget.Amount - budget.CurrentSpent,
            PercentageUsed = Math.Round(percentageUsed, 1),
            StartDate = budget.StartDate,
            EndDate = budget.EndDate,
            AlertThreshold = budget.AlertThreshold,
            LastAlertSent = budget.LastAlertSent,
            Status = status
        };
    }
}
