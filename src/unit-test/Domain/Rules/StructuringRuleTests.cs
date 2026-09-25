using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Domain.Rules;
using fraud_rule_engine_service.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Domain.Rules;

public class StructuringRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static StructuringRule CreateRule() => new(Options.Create(new FraudRuleOptions
    {
        StructuringSubThreshold = 10_000m,
        StructuringAggregateThreshold = 25_000m,
        StructuringWindowMinutes = 60
    }));

    [Fact]
    public void Evaluate_SingleSubThresholdTransaction_DoesNotTrigger()
    {
        var rule = CreateRule();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 9_000m, occurredAt: Now),
            RecentHistory = []
        });

        result.Triggered.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_ManySubThresholdTransactionsExceedingAggregate_Triggers()
    {
        var rule = CreateRule();
        var history = Enumerable.Range(1, 3)
            .Select(i => TransactionFactory.Default(transactionId: $"h-{i}", amount: 8_000m, occurredAt: Now.AddMinutes(-i * 10)))
            .ToList();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", amount: 8_000m, occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeTrue();
        result.RuleCode.ShouldBe("STRUCTURING");
    }

    [Fact]
    public void Evaluate_CurrentTransactionAboveSubThreshold_DoesNotTrigger()
    {
        var rule = CreateRule();
        var history = Enumerable.Range(1, 3)
            .Select(i => TransactionFactory.Default(transactionId: $"h-{i}", amount: 8_000m, occurredAt: Now.AddMinutes(-i * 10)))
            .ToList();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", amount: 15_000m, occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_SubThresholdTransactionsOutsideWindow_AreExcludedFromAggregate()
    {
        var rule = CreateRule();
        var history = Enumerable.Range(1, 3)
            .Select(i => TransactionFactory.Default(transactionId: $"h-{i}", amount: 8_000m, occurredAt: Now.AddHours(-2)))
            .ToList();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", amount: 8_000m, occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeFalse();
    }
}
