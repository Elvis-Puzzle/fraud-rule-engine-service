using fraud_rule_engine_service.Common;
using fraud_rule_engine_service.Common.Exceptions;
using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fraud_rule_engine_service.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/fraud-cases")]
[Produces("application/json")]
public sealed class FraudCasesController(IFraudCaseQueryService queryService) : ControllerBase
{
    /// <summary>Retrieves a single fraud case, including every rule that triggered it.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FraudCaseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FraudCaseResult>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await queryService.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Fraud case '{id}' was not found.");

        return Ok(result);
    }

    /// <summary>Retrieves every fraud case ever raised for a given transaction.</summary>
    [HttpGet("by-transaction/{transactionId}")]
    [ProducesResponseType(typeof(IReadOnlyList<FraudCaseResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FraudCaseResult>>> GetByTransactionId(
        string transactionId, CancellationToken cancellationToken)
    {
        var results = await queryService.GetByTransactionIdAsync(transactionId, cancellationToken);
        return Ok(results);
    }

    /// <summary>Searches fraud cases with optional filters, sorted newest-first and paginated.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FraudCaseResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<FraudCaseResult>>> Search(
        [FromQuery] string? accountId,
        [FromQuery] string? customerId,
        [FromQuery] FraudSeverity? minSeverity,
        [FromQuery] FraudCaseStatus? status,
        [FromQuery] string? ruleCode,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (from is not null && to is not null && from > to)
        {
            return BadRequest(new ProblemDetails { Title = "'from' must not be later than 'to'." });
        }

        var query = new FraudCaseQuery
        {
            AccountId = accountId,
            CustomerId = customerId,
            MinSeverity = minSeverity,
            Status = status,
            RuleCode = ruleCode,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize
        };

        var result = await queryService.QueryAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Aggregate case counts by severity and by triggered rule over a date range, for dashboards/reporting.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(FraudCaseSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<FraudCaseSummary>> GetSummary(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var effectiveTo = to ?? DateTimeOffset.UtcNow;
        var effectiveFrom = from ?? effectiveTo.AddDays(-7);

        var summary = await queryService.GetSummaryAsync(effectiveFrom, effectiveTo, cancellationToken);
        return Ok(summary);
    }

    /// <summary>Transitions a fraud case's investigation status (e.g. after manual review). Restricted to fraud analysts — this is a case-management action, not a read.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.FraudAnalyst)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateFraudCaseStatusRequest request, CancellationToken cancellationToken)
    {
        var updated = await queryService.UpdateStatusAsync(id, request.Status, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}

public sealed record UpdateFraudCaseStatusRequest
{
    public required FraudCaseStatus Status { get; init; }
}
