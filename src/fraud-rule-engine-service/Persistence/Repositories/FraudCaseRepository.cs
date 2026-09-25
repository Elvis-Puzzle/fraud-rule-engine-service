using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace fraud_rule_engine_service.Persistence.Repositories;

public sealed class FraudCaseRepository(ApplicationReadWriteContext context) : IFraudCaseRepository
{
    public Task<bool> TransactionAlreadyProcessedAsync(string transactionId, CancellationToken cancellationToken) =>
        context.TransactionSnapshots.AnyAsync(t => t.TransactionId == transactionId, cancellationToken);

    public async Task<FraudCaseResult?> GetExistingFraudCaseAsync(string transactionId, CancellationToken cancellationToken)
    {
        var entity = await context.FraudCases
            .Include(f => f.TriggeredRules)
            .FirstOrDefaultAsync(f => f.TransactionId == transactionId, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task RecordTransactionAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        context.TransactionSnapshots.Add(transaction.ToSnapshotEntity());
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetRecentHistoryAsync(
        string accountId, DateTimeOffset since, CancellationToken cancellationToken)
    {
        var entities = await context.TransactionSnapshots
            .Where(t => t.AccountId == accountId && t.OccurredAt >= since)
            .OrderByDescending(t => t.OccurredAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<Guid> SaveFraudCaseAsync(
        FraudCaseResult result, string? correlationId, CancellationToken cancellationToken)
    {
        var entity = new FraudCaseEntity
        {
            Id = result.Id,
            TransactionId = result.TransactionId,
            AccountId = result.AccountId,
            CustomerId = result.CustomerId,
            Amount = result.Amount,
            Currency = result.Currency,
            Category = result.Category,
            Severity = result.Severity,
            OverallScore = result.OverallScore,
            Status = FraudCaseStatus.Open,
            CorrelationId = correlationId,
            OccurredAt = result.OccurredAt,
            EvaluatedAt = result.EvaluatedAt,
            TriggeredRules = result.TriggeredRules.Select(r => new FraudCaseTriggeredRuleEntity
            {
                Id = Guid.NewGuid(),
                FraudCaseId = result.Id,
                RuleCode = r.RuleCode,
                RuleName = r.RuleName,
                Score = r.Score,
                Reason = r.Reason ?? string.Empty
            }).ToList()
        };

        context.FraudCases.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, FraudCaseStatus status, CancellationToken cancellationToken)
    {
        var entity = await context.FraudCases.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.Status = status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
