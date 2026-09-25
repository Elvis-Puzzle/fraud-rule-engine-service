using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Domain.Rules;
using fraud_rule_engine_service.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Domain.Rules;

public class NewPayeeLargeTransferRuleTests
{
    private static NewPayeeLargeTransferRule CreateRule() =>
        new(Options.Create(new FraudRuleOptions { NewPayeeAmountThreshold = 15_000m }));

    [Fact]
    public void Evaluate_NewPayeeAboveThreshold_Triggers()
    {
        var rule = CreateRule();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 20_000m, isNewPayee: true, counterpartyAccountId: "payee-1"),
            RecentHistory = []
        });

        result.Triggered.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_ExistingPayeeAboveThreshold_DoesNotTrigger()
    {
        var rule = CreateRule();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 20_000m, isNewPayee: false, counterpartyAccountId: "payee-1"),
            RecentHistory = []
        });

        result.Triggered.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_NewPayeeBelowThreshold_DoesNotTrigger()
    {
        var rule = CreateRule();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(amount: 500m, isNewPayee: true, counterpartyAccountId: "payee-1"),
            RecentHistory = []
        });

        result.Triggered.ShouldBeFalse();
    }
}
