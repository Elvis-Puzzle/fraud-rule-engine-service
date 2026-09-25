using fraud_rule_engine_service.Domain.Rules;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Domain.Rules;

public class FraudRuleOptionsValidatorTests
{
    private readonly FraudRuleOptionsValidator _validator = new();

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var result = _validator.Validate(null, new FraudRuleOptions());

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_SubThresholdAboveAggregateThreshold_Fails()
    {
        var options = new FraudRuleOptions
        {
            StructuringSubThreshold = 30_000m,
            StructuringAggregateThreshold = 25_000m
        };

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains(nameof(FraudRuleOptions.StructuringSubThreshold)));
    }

    [Fact]
    public void Validate_SubThresholdEqualToAggregateThreshold_Fails()
    {
        var options = new FraudRuleOptions
        {
            StructuringSubThreshold = 25_000m,
            StructuringAggregateThreshold = 25_000m
        };

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_UnusualHourStartEqualsEnd_Fails()
    {
        var options = new FraudRuleOptions
        {
            UnusualHourStartUtc = 5,
            UnusualHourEndUtc = 5
        };

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains(nameof(FraudRuleOptions.UnusualHourStartUtc)));
    }
}
