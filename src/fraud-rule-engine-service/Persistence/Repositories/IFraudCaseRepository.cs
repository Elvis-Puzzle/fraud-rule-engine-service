using fraud_rule_engine_service.Domain.Models;

namespace fraud_rule_engine_service.Persistence.Repositories;

/// <summary>Write-side repository, backed by the read/write connection. Used only by the ingestion (Kafka) path.</summary>
public interface IFraudCaseRepository
{
    /// <summary>
    /// True if this transaction has already been recorded — i.e. this call is a Kafka redelivery
    /// of a message that was already fully processed, not a genuinely new transaction.
    /// </summary>
    Task<bool> TransactionAlreadyProcessedAsync(string transactionId, CancellationToken cancellationToken);

    Task RecordTransactionAsync(Transaction transaction, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> GetRecentHistoryAsync(string accountId, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>The fraud case previously raised for this transaction, if any — used to answer a redelivery without re-evaluating.</summary>
    Task<FraudCaseResult?> GetExistingFraudCaseAsync(string transactionId, CancellationToken cancellationToken);

    Task<Guid> SaveFraudCaseAsync(FraudCaseResult result, string? correlationId, CancellationToken cancellationToken);

    Task<bool> UpdateStatusAsync(Guid id, FraudCaseStatus status, CancellationToken cancellationToken);
}
