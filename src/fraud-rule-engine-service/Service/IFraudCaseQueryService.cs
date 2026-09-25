using fraud_rule_engine_service.Domain.Models;

namespace fraud_rule_engine_service.Service;

public interface IFraudCaseQueryService
{
    Task<FraudCaseResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<FraudCaseResult>> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken);

    Task<PagedResult<FraudCaseResult>> QueryAsync(FraudCaseQuery query, CancellationToken cancellationToken);

    Task<FraudCaseSummary> GetSummaryAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<bool> UpdateStatusAsync(Guid id, FraudCaseStatus status, CancellationToken cancellationToken);
}
