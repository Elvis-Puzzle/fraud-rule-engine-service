using fraud_rule_engine_service.Domain.Models;

namespace fraud_rule_engine_service.Domain.Rules;

/// <summary>
/// One independently testable piece of fraud criteria. Every rule looks only at the shared
/// evaluation context (current transaction + recent history) — no rule talks to the database
/// or Kafka directly, so rules can be unit tested with plain in-memory contexts.
/// </summary>
public interface IFraudRule
{
    string Code { get; }
    string Name { get; }
    RuleEvaluationResult Evaluate(TransactionEvaluationContext context);
}
