using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Domain.Rules;

/// <summary>
/// Catches nonsensical relationships between two settings that [Range] can't (e.g. a structuring
/// sub-threshold configured above the aggregate threshold). Wired into ValidateOnStart() alongside
/// DataAnnotations validation.
/// </summary>
public sealed class FraudRuleOptionsValidator : IValidateOptions<FraudRuleOptions>
{
    public ValidateOptionsResult Validate(string? name, FraudRuleOptions options)
    {
        var failures = new List<string>();

        if (options.StructuringSubThreshold >= options.StructuringAggregateThreshold)
        {
            failures.Add(
                $"{nameof(FraudRuleOptions.StructuringSubThreshold)} ({options.StructuringSubThreshold}) must be less than " +
                $"{nameof(FraudRuleOptions.StructuringAggregateThreshold)} ({options.StructuringAggregateThreshold}) — otherwise a single " +
                "sub-threshold transaction could already exceed the aggregate, or the rule could never accumulate correctly.");
        }

        if (options.UnusualHourStartUtc == options.UnusualHourEndUtc)
        {
            failures.Add(
                $"{nameof(FraudRuleOptions.UnusualHourStartUtc)} and {nameof(FraudRuleOptions.UnusualHourEndUtc)} are both " +
                $"{options.UnusualHourStartUtc} — that describes either a zero-length or a full 24-hour window, neither of which is a valid 'unusual hour' range.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
