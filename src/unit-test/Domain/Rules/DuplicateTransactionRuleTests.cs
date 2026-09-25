using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Domain.Rules;
using fraud_rule_engine_service.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Domain.Rules;

public class DuplicateTransactionRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static DuplicateTransactionRule CreateRule() =>
        new(Options.Create(new FraudRuleOptions { DuplicateWindowMinutes = 2 }));

    [Fact]
    public void Evaluate_MatchingTransactionWithinWindow_Triggers()
    {
        var rule = CreateRule();
        var history = new[]
        {
            TransactionFactory.Default(transactionId: "h-1", amount: 500m, counterpartyAccountId: "payee-1", occurredAt: Now.AddSeconds(-30))
        };

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", amount: 500m, counterpartyAccountId: "payee-1", occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_DifferentAmount_DoesNotTrigger()
    {
        var rule = CreateRule();
        var history = new[]
        {
            TransactionFactory.Default(transactionId: "h-1", amount: 500m, counterpartyAccountId: "payee-1", occurredAt: Now.AddSeconds(-30))
        };

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", amount: 600m, counterpartyAccountId: "payee-1", occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_MatchingTransactionOutsideWindow_DoesNotTrigger()
    {
        var rule = CreateRule();
        var history = new[]
        {
            TransactionFactory.Default(transactionId: "h-1", amount: 500m, counterpartyAccountId: "payee-1", occurredAt: Now.AddMinutes(-10))
        };

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", amount: 500m, counterpartyAccountId: "payee-1", occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeFalse();
    }
}
