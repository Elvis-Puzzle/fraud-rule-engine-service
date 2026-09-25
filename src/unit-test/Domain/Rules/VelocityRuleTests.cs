using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Domain.Rules;
using fraud_rule_engine_service.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Domain.Rules;

public class VelocityRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static VelocityRule CreateRule(int windowMinutes = 10, int maxTransactions = 5) =>
        new(Options.Create(new FraudRuleOptions
        {
            VelocityWindowMinutes = windowMinutes,
            VelocityMaxTransactions = maxTransactions
        }));

    [Fact]
    public void Evaluate_FewTransactionsInWindow_DoesNotTrigger()
    {
        var rule = CreateRule(windowMinutes: 10, maxTransactions: 5);
        var history = Enumerable.Range(0, 2)
            .Select(i => TransactionFactory.Default(transactionId: $"h-{i}", occurredAt: Now.AddMinutes(-i)))
            .ToList();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_TransactionCountReachesLimitWithinWindow_Triggers()
    {
        var rule = CreateRule(windowMinutes: 10, maxTransactions: 5);
        var history = Enumerable.Range(1, 4)
            .Select(i => TransactionFactory.Default(transactionId: $"h-{i}", occurredAt: Now.AddMinutes(-i)))
            .ToList();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_TransactionsOutsideWindow_AreIgnored()
    {
        var rule = CreateRule(windowMinutes: 10, maxTransactions: 5);
        var history = Enumerable.Range(1, 10)
            .Select(i => TransactionFactory.Default(transactionId: $"h-{i}", occurredAt: Now.AddMinutes(-30 - i)))
            .ToList();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_OtherAccountsTransactions_AreIgnored()
    {
        var rule = CreateRule(windowMinutes: 10, maxTransactions: 5);
        var history = Enumerable.Range(1, 10)
            .Select(i => TransactionFactory.Default(transactionId: $"h-{i}", accountId: "other-acc", occurredAt: Now.AddMinutes(-i)))
            .ToList();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(transactionId: "current", accountId: "acc-1", occurredAt: Now),
            RecentHistory = history
        });

        result.Triggered.ShouldBeFalse();
    }
}
