using fraud_rule_engine_service.Domain.Models;

namespace fraud_rule_engine_service.Service;

public interface IFraudEvaluationService
{
    /// <summary>
    /// Evaluates a transaction against every registered fraud rule, records it in the account's
    /// history, and persists a fraud case if at least one rule triggers.
    /// </summary>
    Task<FraudCaseResult?> EvaluateAsync(Transaction transaction, string? correlationId, CancellationToken cancellationToken);
}
