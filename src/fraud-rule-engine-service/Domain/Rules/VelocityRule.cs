using fraud_rule_engine_service.Domain.Models;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Domain.Rules;

/// <summary>Flags an account transacting too many times in a short sliding window.</summary>
public sealed class VelocityRule(IOptions<FraudRuleOptions> options) : IFraudRule
{
    private readonly FraudRuleOptions _options = options.Value;

    public string Code => "VELOCITY";
    public string Name => "Transaction Velocity";

    public RuleEvaluationResult Evaluate(TransactionEvaluationContext context)
    {
        var windowStart = context.Current.OccurredAt.AddMinutes(-_options.VelocityWindowMinutes);
        var countInWindow = context.RecentHistory
            .Count(t => t.AccountId == context.Current.AccountId && t.OccurredAt >= windowStart)
            + 1; // include the current transaction

        if (countInWindow < _options.VelocityMaxTransactions)
        {
            return RuleEvaluationResult.NotTriggered(Code, Name);
        }

        var score = Math.Min(95, 50 + (countInWindow - _options.VelocityMaxTransactions) * 10);
        return RuleEvaluationResult.Trigger(Code, Name, score,
            $"{countInWindow} transactions on account {context.Current.AccountId} within {_options.VelocityWindowMinutes} minutes (limit {_options.VelocityMaxTransactions}).");
    }
}
