using fraud_rule_engine_service.Domain.Models;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Domain.Rules;

/// <summary>
/// Flags a transaction that looks like an accidental or malicious replay: same account, amount
/// and counterparty repeated within a very short window.
/// </summary>
public sealed class DuplicateTransactionRule(IOptions<FraudRuleOptions> options) : IFraudRule
{
    private readonly FraudRuleOptions _options = options.Value;

    public string Code => "DUPLICATE";
    public string Name => "Duplicate Transaction";

    public RuleEvaluationResult Evaluate(TransactionEvaluationContext context)
    {
        var current = context.Current;
        var windowStart = current.OccurredAt.AddMinutes(-_options.DuplicateWindowMinutes);

        var duplicate = context.RecentHistory.Any(t =>
            t.AccountId == current.AccountId
            && t.TransactionId != current.TransactionId
            && t.Amount == current.Amount
            && t.CounterpartyAccountId == current.CounterpartyAccountId
            && t.OccurredAt >= windowStart
            && t.OccurredAt < current.OccurredAt);

        if (!duplicate)
        {
            return RuleEvaluationResult.NotTriggered(Code, Name);
        }

        return RuleEvaluationResult.Trigger(Code, Name, 65,
            $"A matching transaction of {current.Amount:N2} {current.Currency} to the same counterparty occurred within {_options.DuplicateWindowMinutes} minute(s).");
    }
}
