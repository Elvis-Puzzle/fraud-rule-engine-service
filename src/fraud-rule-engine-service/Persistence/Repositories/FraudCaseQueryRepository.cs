using fraud_rule_engine_service.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace fraud_rule_engine_service.Persistence.Repositories;

public sealed class FraudCaseQueryRepository(ApplicationReadOnlyContext context) : IFraudCaseQueryRepository
{
    public async Task<FraudCaseResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await context.FraudCases
            .Include(f => f.TriggeredRules)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<FraudCaseResult>> GetByTransactionIdAsync(
        string transactionId, CancellationToken cancellationToken)
    {
        var entities = await context.FraudCases
            .Include(f => f.TriggeredRules)
            .Where(f => f.TransactionId == transactionId)
            .OrderByDescending(f => f.EvaluatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<PagedResult<FraudCaseResult>> QueryAsync(FraudCaseQuery query, CancellationToken cancellationToken)
    {
        var filtered = context.FraudCases.Include(f => f.TriggeredRules).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.AccountId))
        {
            filtered = filtered.Where(f => f.AccountId == query.AccountId);
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerId))
        {
            filtered = filtered.Where(f => f.CustomerId == query.CustomerId);
        }

        if (query.MinSeverity is { } minSeverity)
        {
            filtered = filtered.Where(f => f.Severity >= minSeverity);
        }

        if (query.Status is { } status)
        {
            filtered = filtered.Where(f => f.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.RuleCode))
        {
            filtered = filtered.Where(f => f.TriggeredRules.Any(r => r.RuleCode == query.RuleCode));
        }

        if (query.From is { } from)
        {
            filtered = filtered.Where(f => f.EvaluatedAt >= from);
        }

        if (query.To is { } to)
        {
            filtered = filtered.Where(f => f.EvaluatedAt <= to);
        }

        var totalCount = await filtered.CountAsync(cancellationToken);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var entities = await filtered
            .OrderByDescending(f => f.EvaluatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FraudCaseResult>
        {
            Items = entities.Select(e => e.ToDomain()).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<FraudCaseSummary> GetSummaryAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var casesInRange = context.FraudCases
            .Where(f => f.EvaluatedAt >= from && f.EvaluatedAt <= to);

        var totalCases = await casesInRange.CountAsync(cancellationToken);

        var bySeverity = await casesInRange
            .GroupBy(f => f.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byRule = await context.FraudCaseTriggeredRules
            .Where(r => r.FraudCase != null && r.FraudCase.EvaluatedAt >= from && r.FraudCase.EvaluatedAt <= to)
            .GroupBy(r => r.RuleCode)
            .Select(g => new { RuleCode = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new FraudCaseSummary
        {
            From = from,
            To = to,
            TotalCases = totalCases,
            CasesBySeverity = bySeverity.ToDictionary(x => x.Severity, x => x.Count),
            CasesByRule = byRule.ToDictionary(x => x.RuleCode, x => x.Count)
        };
    }
}
