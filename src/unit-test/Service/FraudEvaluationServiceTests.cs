using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Domain.Rules;
using fraud_rule_engine_service.Persistence.Repositories;
using fraud_rule_engine_service.Service;
using fraud_rule_engine_service.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Service;

public class FraudEvaluationServiceTests
{
    private readonly Mock<IFraudCaseRepository> _repository = new();
    private readonly FraudRuleOptions _options = new();

    private FraudEvaluationService CreateService(IEnumerable<IFraudRule> rules)
    {
        _repository
            .Setup(r => r.GetRecentHistoryAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        return new FraudEvaluationService(rules, _repository.Object, Options.Create(_options), new LoggerFactory().CreateLogger<FraudEvaluationService>());
    }

    [Fact]
    public async Task EvaluateAsync_NoRulesTrigger_ReturnsNullAndDoesNotSaveFraudCase()
    {
        var rule = Mock.Of<IFraudRule>(r =>
            r.Code == "NOOP" && r.Name == "No Op" &&
            r.Evaluate(It.IsAny<TransactionEvaluationContext>()) == RuleEvaluationResult.NotTriggered("NOOP", "No Op"));
        var service = CreateService([rule]);

        var result = await service.EvaluateAsync(TransactionFactory.Default(), correlationId: null, CancellationToken.None);

        result.ShouldBeNull();
        _repository.Verify(r => r.SaveFraudCaseAsync(It.IsAny<FraudCaseResult>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_OneRuleTriggers_SavesFraudCaseWithMatchingSeverity()
    {
        var rule = Mock.Of<IFraudRule>(r =>
            r.Code == "HIGH_VALUE" && r.Name == "High Value" &&
            r.Evaluate(It.IsAny<TransactionEvaluationContext>()) == RuleEvaluationResult.Trigger("HIGH_VALUE", "High Value", 70, "too high"));
        var service = CreateService([rule]);

        var result = await service.EvaluateAsync(TransactionFactory.Default(), correlationId: "corr-1", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Severity.ShouldBe(FraudSeverity.High);
        result.TriggeredRules.Count.ShouldBe(1);
        _repository.Verify(r => r.SaveFraudCaseAsync(It.IsAny<FraudCaseResult>(), "corr-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_AlwaysRecordsTheTransactionRegardlessOfOutcome()
    {
        var rule = Mock.Of<IFraudRule>(r =>
            r.Evaluate(It.IsAny<TransactionEvaluationContext>()) == RuleEvaluationResult.NotTriggered("NOOP", "No Op"));
        var service = CreateService([rule]);
        var transaction = TransactionFactory.Default();

        await service.EvaluateAsync(transaction, correlationId: null, CancellationToken.None);

        _repository.Verify(r => r.RecordTransactionAsync(transaction, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_MultipleRulesTrigger_OverallScoreAccountsForAllOfThem()
    {
        var ruleA = Mock.Of<IFraudRule>(r =>
            r.Evaluate(It.IsAny<TransactionEvaluationContext>()) == RuleEvaluationResult.Trigger("A", "A", 50, "a"));
        var ruleB = Mock.Of<IFraudRule>(r =>
            r.Evaluate(It.IsAny<TransactionEvaluationContext>()) == RuleEvaluationResult.Trigger("B", "B", 40, "b"));
        var service = CreateService([ruleA, ruleB]);

        var result = await service.EvaluateAsync(TransactionFactory.Default(), correlationId: null, CancellationToken.None);

        result.ShouldNotBeNull();
        result.OverallScore.ShouldBe(55); // max(50, 40) + (2 - 1) * 5
        result.TriggeredRules.Count.ShouldBe(2);
    }

    // Guards against a Kafka redelivery hitting the unique index and getting wrongly DLQ'd.
    [Fact]
    public async Task EvaluateAsync_TransactionAlreadyProcessed_ReturnsExistingCaseWithoutReRunningRules()
    {
        var rule = new Mock<IFraudRule>();
        rule.Setup(r => r.Evaluate(It.IsAny<TransactionEvaluationContext>()))
            .Returns(RuleEvaluationResult.Trigger("HIGH_VALUE", "High Value", 70, "too high"));

        var existingCase = new FraudCaseResult
        {
            Id = Guid.NewGuid(),
            TransactionId = "txn-1",
            AccountId = "acc-1",
            CustomerId = "cust-1",
            Amount = 100m,
            Currency = "ZAR",
            Category = "groceries",
            Severity = FraudSeverity.High,
            OverallScore = 70,
            Status = FraudCaseStatus.Open,
            OccurredAt = DateTimeOffset.UtcNow,
            EvaluatedAt = DateTimeOffset.UtcNow,
            TriggeredRules = []
        };

        _repository
            .Setup(r => r.TransactionAlreadyProcessedAsync("txn-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repository
            .Setup(r => r.GetExistingFraudCaseAsync("txn-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCase);

        var service = CreateService([rule.Object]);
        var result = await service.EvaluateAsync(
            TransactionFactory.Default(transactionId: "txn-1"), correlationId: null, CancellationToken.None);

        result.ShouldBe(existingCase);
        rule.Verify(r => r.Evaluate(It.IsAny<TransactionEvaluationContext>()), Times.Never);
        _repository.Verify(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveFraudCaseAsync(It.IsAny<FraudCaseResult>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_TransactionAlreadyProcessedButNeverRaisedACase_ReturnsNull()
    {
        _repository
            .Setup(r => r.TransactionAlreadyProcessedAsync("txn-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repository
            .Setup(r => r.GetExistingFraudCaseAsync("txn-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FraudCaseResult?)null);

        var rule = Mock.Of<IFraudRule>(r =>
            r.Evaluate(It.IsAny<TransactionEvaluationContext>()) == RuleEvaluationResult.NotTriggered("NOOP", "No Op"));
        var service = CreateService([rule]);

        var result = await service.EvaluateAsync(
            TransactionFactory.Default(transactionId: "txn-2"), correlationId: null, CancellationToken.None);

        result.ShouldBeNull();
        _repository.Verify(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
