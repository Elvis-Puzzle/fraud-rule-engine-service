using fraud_rule_engine_service.Domain.Models;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Domain.Rules;

/// <summary>Flags transactions against a merchant or merchant category code on the configured blacklist.</summary>
public sealed class BlacklistedMerchantRule(IOptions<FraudRuleOptions> options) : IFraudRule
{
    private readonly FraudRuleOptions _options = options.Value;

    public string Code => "BLACKLISTED_MERCHANT";
    public string Name => "Blacklisted Merchant";

    public RuleEvaluationResult Evaluate(TransactionEvaluationContext context)
    {
        var current = context.Current;

        var mccHit = current.MerchantCategoryCode is not null
            && _options.BlacklistedMerchantCategoryCodes.Contains(current.MerchantCategoryCode, StringComparer.OrdinalIgnoreCase);
        var nameHit = current.MerchantName is not null
            && _options.BlacklistedMerchantNames.Any(n => current.MerchantName.Contains(n, StringComparison.OrdinalIgnoreCase));

        if (!mccHit && !nameHit)
        {
            return RuleEvaluationResult.NotTriggered(Code, Name);
        }

        return RuleEvaluationResult.Trigger(Code, Name, 80,
            $"Merchant '{current.MerchantName}' (MCC {current.MerchantCategoryCode}) matches the blacklist.");
    }
}
