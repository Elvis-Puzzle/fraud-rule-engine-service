using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Domain.Rules;
using fraud_rule_engine_service.Persistence.Repositories;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Service;

public sealed class FraudEvaluationService(
    IEnumerable<IFraudRule> rules,
    IFraudCaseRepository repository,
    IOptions<FraudRuleOptions> options,
    ILogger<FraudEvaluationService> logger) : IFraudEvaluationService
{
    private readonly FraudRuleOptions _options = options.Value;

    public async Task<FraudCaseResult?> EvaluateAsync(
        Transaction transaction, string? correlationId, CancellationToken cancellationToken)
    {
        // Kafka is at-least-once, so a redelivered transaction would otherwise hit the unique
        // index in RecordTransactionAsync and get wrongly routed to the DLQ.
        if (await repository.TransactionAlreadyProcessedAsync(transaction.TransactionId, cancellationToken))
        {
            logger.LogInformation(
                "Transaction {TransactionId} was already processed — treating this as a Kafka redelivery, not re-evaluating.",
                transaction.TransactionId);
            return await repository.GetExistingFraudCaseAsync(transaction.TransactionId, cancellationToken);
        }

        var since = transaction.OccurredAt.AddMinutes(-_options.HistoryLookbackMinutes);
        var history = await repository.GetRecentHistoryAsync(transaction.AccountId, since, cancellationToken);

        var context = new TransactionEvaluationContext
        {
            Current = transaction,
            RecentHistory = history
        };

        var triggered = rules
            .Select(rule => rule.Evaluate(context))
            .Where(result => result.Triggered)
            .ToList();

        // Record the transaction itself only after evaluation, so it never counts towards its own history.
        await repository.RecordTransactionAsync(transaction, cancellationToken);

        if (triggered.Count == 0)
        {
            return null;
        }

        var overallScore = Math.Min(100, triggered.Max(r => r.Score) + (triggered.Count - 1) * 5);
        var severity = ToSeverity(overallScore);

        var result = new FraudCaseResult
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.TransactionId,
            AccountId = transaction.AccountId,
            CustomerId = transaction.CustomerId,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            Category = transaction.Category,
            Severity = severity,
            OverallScore = overallScore,
            Status = FraudCaseStatus.Open,
            OccurredAt = transaction.OccurredAt,
            EvaluatedAt = DateTimeOffset.UtcNow,
            TriggeredRules = triggered
        };

        await repository.SaveFraudCaseAsync(result, correlationId, cancellationToken);

        logger.LogWarning(
            "Fraud case {FraudCaseId} raised for transaction {TransactionId} on account {AccountId}: {TriggeredRuleCount} rule(s) triggered, severity {Severity}, score {Score}.",
            result.Id, transaction.TransactionId, transaction.AccountId, triggered.Count, severity, overallScore);

        return result;
    }

    private static FraudSeverity ToSeverity(int score) => score switch
    {
        >= 85 => FraudSeverity.Critical,
        >= 65 => FraudSeverity.High,
        >= 40 => FraudSeverity.Medium,
        > 0 => FraudSeverity.Low,
        _ => FraudSeverity.None
    };
}
