using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Domain.Rules;
using fraud_rule_engine_service.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Domain.Rules;

public class UnusualHourRuleTests
{
    private static UnusualHourRule CreateRule() => new(Options.Create(new FraudRuleOptions
    {
        UnusualHourStartUtc = 0,
        UnusualHourEndUtc = 4,
        UnusualHourAmountThreshold = 5_000m
    }));

    [Fact]
    public void Evaluate_LargeAmountDuringUnusualHour_Triggers()
    {
        var rule = CreateRule();
        var occurredAt = new DateTimeOffset(2026, 1, 1, 2, 0, 0, TimeSpan.Zero);

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 10_000m, occurredAt: occurredAt),
            RecentHistory = []
        });

        result.Triggered.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_LargeAmountDuringNormalHour_DoesNotTrigger()
    {
        var rule = CreateRule();
        var occurredAt = new DateTimeOffset(2026, 1, 1, 14, 0, 0, TimeSpan.Zero);

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 10_000m, occurredAt: occurredAt),
            RecentHistory = []
        });

        result.Triggered.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_SmallAmountDuringUnusualHour_DoesNotTrigger()
    {
        var rule = CreateRule();
        var occurredAt = new DateTimeOffset(2026, 1, 1, 2, 0, 0, TimeSpan.Zero);

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 100m, occurredAt: occurredAt),
            RecentHistory = []
        });

        result.Triggered.ShouldBeFalse();
    }

    // The window can wrap past midnight (e.g. 22:00-04:00), which flips the rule's comparison
    // from "start <= hour < end" to "hour >= start || hour < end" — an entirely different branch
    // that a straight 0-4 window (used by every other test above) never exercises.
    private static UnusualHourRule CreateWrapAroundRule() => new(Options.Create(new FraudRuleOptions
    {
        UnusualHourStartUtc = 22,
        UnusualHourEndUtc = 4,
        UnusualHourAmountThreshold = 5_000m
    }));

    [Theory]
    [InlineData(23)] // after the wrap point, before midnight
    [InlineData(0)]  // exactly midnight
    [InlineData(3)]  // after midnight, still before the end hour
    public void Evaluate_WrapAroundWindow_LargeAmountInsideWindow_Triggers(int hour)
    {
        var rule = CreateWrapAroundRule();
        var occurredAt = new DateTimeOffset(2026, 1, 1, hour, 0, 0, TimeSpan.Zero);

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 10_000m, occurredAt: occurredAt),
            RecentHistory = []
        });

        result.Triggered.ShouldBeTrue();
    }

    [Theory]
    [InlineData(4)]  // exactly the end hour — already outside the window
    [InlineData(12)] // the middle of the day — clearly outside
    [InlineData(21)] // just before the wrap point
    public void Evaluate_WrapAroundWindow_LargeAmountOutsideWindow_DoesNotTrigger(int hour)
    {
        var rule = CreateWrapAroundRule();
        var occurredAt = new DateTimeOffset(2026, 1, 1, hour, 0, 0, TimeSpan.Zero);

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 10_000m, occurredAt: occurredAt),
            RecentHistory = []
        });

        result.Triggered.ShouldBeFalse();
    }
}
