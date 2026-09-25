using fraud_rule_engine_service.Domain.Models;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Domain.Rules;

/// <summary>
/// Flags two transactions on the same account, from different countries, close enough together
/// in time that physical travel between them would not have been possible.
/// </summary>
public sealed class ImpossibleTravelRule(IOptions<FraudRuleOptions> options) : IFraudRule
{
    private readonly FraudRuleOptions _options = options.Value;

    public string Code => "IMPOSSIBLE_TRAVEL";
    public string Name => "Impossible Travel";

    public RuleEvaluationResult Evaluate(TransactionEvaluationContext context)
    {
        var current = context.Current;
        var windowStart = current.OccurredAt.AddMinutes(-_options.ImpossibleTravelWindowMinutes);

        var priorDifferentCountry = context.RecentHistory
            .Where(t => t.AccountId == current.AccountId
                        && t.OccurredAt >= windowStart
                        && t.OccurredAt < current.OccurredAt
                        && !string.Equals(t.CountryCode, current.CountryCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.OccurredAt)
            .FirstOrDefault();

        if (priorDifferentCountry is null)
        {
            return RuleEvaluationResult.NotTriggered(Code, Name);
        }

        var gap = current.OccurredAt - priorDifferentCountry.OccurredAt;
        return RuleEvaluationResult.Trigger(Code, Name, 85,
            $"Account {current.AccountId} transacted in {priorDifferentCountry.CountryCode} then {current.CountryCode} only {gap.TotalMinutes:N0} minutes apart.");
    }
}
