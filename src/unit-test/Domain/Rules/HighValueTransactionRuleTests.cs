using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Domain.Rules;
using fraud_rule_engine_service.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Domain.Rules;

public class HighValueTransactionRuleTests
{
    private static HighValueTransactionRule CreateRule(decimal threshold = 50_000m) =>
        new(Options.Create(new FraudRuleOptions { HighValueThreshold = threshold }));

    [Fact]
    public void Evaluate_AmountBelowThreshold_DoesNotTrigger()
    {
        var rule = CreateRule(threshold: 50_000m);
        var context = new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 1_000m),
            RecentHistory = []
        };

        var result = rule.Evaluate(context);

        result.Triggered.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_AmountAtThreshold_Triggers()
    {
        var rule = CreateRule(threshold: 50_000m);
        var context = new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 50_000m),
            RecentHistory = []
        };

        var result = rule.Evaluate(context);

        result.Triggered.ShouldBeTrue();
        result.RuleCode.ShouldBe("HIGH_VALUE");
    }

    [Fact]
    public void Evaluate_AmountWellAboveThreshold_ScoresHigherThanJustOverThreshold()
    {
        var rule = CreateRule(threshold: 50_000m);
        var justOver = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 50_001m),
            RecentHistory = []
        });
        var wellOver = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 200_000m),
            RecentHistory = []
        });

        wellOver.Score.ShouldBeGreaterThan(justOver.Score);
    }
}
