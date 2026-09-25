using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Domain.Rules;
using fraud_rule_engine_service.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Domain.Rules;

public class BlacklistedMerchantRuleTests
{
    private static BlacklistedMerchantRule CreateRule() => new(Options.Create(new FraudRuleOptions
    {
        BlacklistedMerchantCategoryCodes = ["7995"],
        BlacklistedMerchantNames = ["Shady Exchange"]
    }));

    [Fact]
    public void Evaluate_BlacklistedMerchantCategoryCode_Triggers()
    {
        var rule = CreateRule();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(merchantCategoryCode: "7995", merchantName: "Some Casino"),
            RecentHistory = []
        });

        result.Triggered.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_BlacklistedMerchantNameSubstring_Triggers()
    {
        var rule = CreateRule();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(merchantCategoryCode: "5411", merchantName: "Shady Exchange Ltd"),
            RecentHistory = []
        });

        result.Triggered.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_UnlistedMerchant_DoesNotTrigger()
    {
        var rule = CreateRule();

        var result = rule.Evaluate(new TransactionEvaluationContext
        {
            Current = TransactionFactory.Default(merchantCategoryCode: "5411", merchantName: "Local Store"),
            RecentHistory = []
        });

        result.Triggered.ShouldBeFalse();
    }
}
