using fraud_rule_engine_service.Domain.Models;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Domain.Rules;

/// <summary>Flags any single transaction at or above the configured high-value threshold.</summary>
public sealed class HighValueTransactionRule(IOptions<FraudRuleOptions> options) : IFraudRule
{
    private readonly FraudRuleOptions _options = options.Value;

    public string Code => "HIGH_VALUE";
    public string Name => "High Value Transaction";

    public RuleEvaluationResult Evaluate(TransactionEvaluationContext context)
    {
        var amount = context.Current.Amount;
        if (amount < _options.HighValueThreshold)
        {
            return RuleEvaluationResult.NotTriggered(Code, Name);
        }

        var score = amount >= _options.HighValueThreshold * 2 ? 90 : 60;
        return RuleEvaluationResult.Trigger(Code, Name, score,
            $"Transaction amount {amount:N2} {context.Current.Currency} exceeds the high-value threshold of {_options.HighValueThreshold:N2}.");
    }
}
