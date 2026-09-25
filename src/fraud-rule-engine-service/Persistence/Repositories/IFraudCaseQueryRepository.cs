using fraud_rule_engine_service.Domain.Models;

namespace fraud_rule_engine_service.Persistence.Repositories;

/// <summary>Read-side repository, backed by the read-only connection. Used only by the retrieval API.</summary>
public interface IFraudCaseQueryRepository
{
    Task<FraudCaseResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<FraudCaseResult>> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken);

    Task<PagedResult<FraudCaseResult>> QueryAsync(FraudCaseQuery query, CancellationToken cancellationToken);

    Task<FraudCaseSummary> GetSummaryAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}
