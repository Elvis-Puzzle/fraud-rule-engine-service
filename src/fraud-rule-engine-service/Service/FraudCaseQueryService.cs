using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Persistence.Repositories;

namespace fraud_rule_engine_service.Service;

public sealed class FraudCaseQueryService(
    IFraudCaseQueryRepository queryRepository,
    IFraudCaseRepository repository) : IFraudCaseQueryService
{
    public Task<FraudCaseResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        queryRepository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<FraudCaseResult>> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken) =>
        queryRepository.GetByTransactionIdAsync(transactionId, cancellationToken);

    public Task<PagedResult<FraudCaseResult>> QueryAsync(FraudCaseQuery query, CancellationToken cancellationToken) =>
        queryRepository.QueryAsync(query, cancellationToken);

    public Task<FraudCaseSummary> GetSummaryAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        queryRepository.GetSummaryAsync(from, to, cancellationToken);

    // Status transitions are a write, so they go through the read/write repository even though this
    // service otherwise fronts the read-only query path.
    public Task<bool> UpdateStatusAsync(Guid id, FraudCaseStatus status, CancellationToken cancellationToken) =>
        repository.UpdateStatusAsync(id, status, cancellationToken);
}
