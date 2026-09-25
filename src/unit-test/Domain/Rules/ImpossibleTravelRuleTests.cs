using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Domain.Rules;
using fraud_rule_engine_service.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Domain.Rules;

public class ImpossibleTravelRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static ImpossibleTravelRule CreateRule(int windowMinutes = 120) =>
        new(Options.Create(new FraudRuleOptions { ImpossibleTravelWindowMinutes = windowMinutes }));

    [Fact]
    public void Evaluate_SameCountryAsRecentHistory_DoesNotTrigger()
    {
        var rule = CreateRule();
        var history = new[] { TransactionFactory.Default(transactionId: "h-1", countryCode: "ZA", occurredAt: Now.AddMinutes(-30)) };

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", countryCode: "ZA", occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_DifferentCountryWithinWindow_Triggers()
    {
        var rule = CreateRule(windowMinutes: 120);
        var history = new[] { TransactionFactory.Default(transactionId: "h-1", countryCode: "GB", occurredAt: Now.AddMinutes(-30)) };

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", countryCode: "ZA", occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeTrue();
        result.RuleCode.ShouldBe("IMPOSSIBLE_TRAVEL");
    }

    [Fact]
    public void Evaluate_DifferentCountryOutsideWindow_DoesNotTrigger()
    {
        var rule = CreateRule(windowMinutes: 120);
        var history = new[] { TransactionFactory.Default(transactionId: "h-1", countryCode: "GB", occurredAt: Now.AddHours(-5)) };

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", countryCode: "ZA", occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeFalse();
    }
}
