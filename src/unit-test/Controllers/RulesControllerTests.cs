using fraud_rule_engine_service.Controllers;
using fraud_rule_engine_service.Domain.Rules;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Controllers;

public class RulesControllerTests
{
    [Fact]
    public void GetAll_ReturnsEveryRegisteredRuleSortedByCode()
    {
        var ruleB = Mock.Of<IFraudRule>(r => r.Code == "B_RULE" && r.Name == "B");
        var ruleA = Mock.Of<IFraudRule>(r => r.Code == "A_RULE" && r.Name == "A");
        var controller = new RulesController([ruleB, ruleA]);

        var result = controller.GetAll();

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var descriptors = ok.Value.ShouldBeAssignableTo<IReadOnlyList<RuleDescriptor>>()!;
        descriptors.Select(d => d.Code).ShouldBe(["A_RULE", "B_RULE"]);
    }
}
