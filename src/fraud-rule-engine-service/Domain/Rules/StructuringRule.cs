using fraud_rule_engine_service.Domain.Models;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Domain.Rules;

/// <summary>
/// Classic "smurfing" pattern: several transactions individually below the sub-threshold, whose
/// sum within a window crosses the aggregate threshold — a common way to avoid single-transaction
/// monitoring.
/// </summary>
public sealed class StructuringRule(IOptions<FraudRuleOptions> options) : IFraudRule
{
    private readonly FraudRuleOptions _options = options.Value;

    public string Code => "STRUCTURING";
    public string Name => "Potential Structuring";

    public RuleEvaluationResult Evaluate(TransactionEvaluationContext context)
    {
        var current = context.Current;
        if (current.Amount >= _options.StructuringSubThreshold)
        {
            return RuleEvaluationResult.NotTriggered(Code, Name);
        }

        var windowStart = current.OccurredAt.AddMinutes(-_options.StructuringWindowMinutes);
        var candidates = context.RecentHistory
            .Where(t => t.AccountId == current.AccountId
                        && t.OccurredAt >= windowStart
                        && t.Amount < _options.StructuringSubThreshold)
            .ToList();

        var total = candidates.Sum(t => t.Amount) + current.Amount;
        if (total < _options.StructuringAggregateThreshold)
        {
            return RuleEvaluationResult.NotTriggered(Code, Name);
        }

        var score = Math.Min(95, 65 + candidates.Count * 5);
        return RuleEvaluationResult.Trigger(Code, Name, score,
            $"{candidates.Count + 1} sub-threshold transactions on account {current.AccountId} totalling {total:N2} {current.Currency} within {_options.StructuringWindowMinutes} minutes (aggregate limit {_options.StructuringAggregateThreshold:N2}).");
    }
}
