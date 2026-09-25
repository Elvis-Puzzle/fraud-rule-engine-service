using fraud_rule_engine_service.Domain.Models;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Domain.Rules;

/// <summary>Flags larger transactions occurring during a configured overnight UTC window.</summary>
public sealed class UnusualHourRule(IOptions<FraudRuleOptions> options) : IFraudRule
{
    private readonly FraudRuleOptions _options = options.Value;

    public string Code => "UNUSUAL_HOUR";
    public string Name => "Unusual Hour Transaction";

    public RuleEvaluationResult Evaluate(TransactionEvaluationContext context)
    {
        var current = context.Current;
        var hour = current.OccurredAt.UtcDateTime.Hour;
        var withinWindow = _options.UnusualHourStartUtc <= _options.UnusualHourEndUtc
            ? hour >= _options.UnusualHourStartUtc && hour < _options.UnusualHourEndUtc
            : hour >= _options.UnusualHourStartUtc || hour < _options.UnusualHourEndUtc;

        if (!withinWindow || current.Amount < _options.UnusualHourAmountThreshold)
        {
            return RuleEvaluationResult.NotTriggered(Code, Name);
        }

        return RuleEvaluationResult.Trigger(Code, Name, 55,
            $"Transaction of {current.Amount:N2} {current.Currency} occurred at {current.OccurredAt.UtcDateTime:HH:mm} UTC, inside the unusual-hour window.");
    }
}
