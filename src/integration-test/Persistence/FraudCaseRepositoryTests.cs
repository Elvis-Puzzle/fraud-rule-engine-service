using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Persistence.Repositories;
using Shouldly;

namespace fraud_rule_engine_service.IntegrationTests.Persistence;

[Collection(nameof(PostgresCollection))]
public class FraudCaseRepositoryTests(PostgresFixture fixture)
{
    private static Transaction BuildTransaction(string transactionId, string accountId, DateTimeOffset occurredAt, decimal amount = 100m) => new()
    {
        TransactionId = transactionId,
        AccountId = accountId,
        CustomerId = "cust-1",
        Amount = amount,
        Currency = "ZAR",
        Category = "groceries",
        Channel = TransactionChannel.Pos,
        CountryCode = "ZA",
        OccurredAt = occurredAt
    };

    [Fact]
    public async Task RecordAndRetrieveHistory_RoundTripsThroughRealPostgres()
    {
        var accountId = $"acc-{Guid.NewGuid()}";
        var now = DateTimeOffset.UtcNow;

        await using var writeContext = fixture.CreateReadWriteContext();
        var repository = new FraudCaseRepository(writeContext);

        await repository.RecordTransactionAsync(BuildTransaction("t1", accountId, now.AddMinutes(-5)), CancellationToken.None);
        await repository.RecordTransactionAsync(BuildTransaction("t2", accountId, now.AddMinutes(-2)), CancellationToken.None);
        await repository.RecordTransactionAsync(BuildTransaction("t3", "different-account", now.AddMinutes(-1)), CancellationToken.None);

        var history = await repository.GetRecentHistoryAsync(accountId, now.AddMinutes(-10), CancellationToken.None);

        history.Count.ShouldBe(2);
        history.ShouldAllBe(t => t.AccountId == accountId);
    }

    [Fact]
    public async Task SaveFraudCase_ThenQuery_ReturnsItWithTriggeredRules()
    {
        var fraudCaseId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var result = new FraudCaseResult
        {
            Id = fraudCaseId,
            TransactionId = $"txn-{fraudCaseId}",
            AccountId = "acc-1",
            CustomerId = "cust-1",
            Amount = 60_000m,
            Currency = "ZAR",
            Category = "transfer",
            Severity = FraudSeverity.High,
            OverallScore = 70,
            Status = FraudCaseStatus.Open,
            OccurredAt = now,
            EvaluatedAt = now,
            TriggeredRules =
            [
                new RuleEvaluationResult { RuleCode = "HIGH_VALUE", RuleName = "High Value", Triggered = true, Score = 70, Reason = "too high" }
            ]
        };

        await using var writeContext = fixture.CreateReadWriteContext();
        var repository = new FraudCaseRepository(writeContext);
        await repository.SaveFraudCaseAsync(result, correlationId: "corr-1", CancellationToken.None);

        await using var readContext = fixture.CreateReadOnlyContext();
        var queryRepository = new FraudCaseQueryRepository(readContext);
        var fetched = await queryRepository.GetByIdAsync(fraudCaseId, CancellationToken.None);

        fetched.ShouldNotBeNull();
        fetched.Severity.ShouldBe(FraudSeverity.High);
        fetched.Category.ShouldBe("transfer");
        fetched.TriggeredRules.Count.ShouldBe(1);
        fetched.TriggeredRules[0].RuleCode.ShouldBe("HIGH_VALUE");
    }

    [Fact]
    public async Task TransactionAlreadyProcessedAsync_ReflectsWhetherTheTransactionWasRecorded()
    {
        var transactionId = $"txn-{Guid.NewGuid()}";
        await using var writeContext = fixture.CreateReadWriteContext();
        var repository = new FraudCaseRepository(writeContext);

        (await repository.TransactionAlreadyProcessedAsync(transactionId, CancellationToken.None)).ShouldBeFalse();

        await repository.RecordTransactionAsync(BuildTransaction(transactionId, "acc-1", DateTimeOffset.UtcNow), CancellationToken.None);

        (await repository.TransactionAlreadyProcessedAsync(transactionId, CancellationToken.None)).ShouldBeTrue();
    }

    [Fact]
    public async Task GetExistingFraudCaseAsync_ReturnsThePreviouslySavedCase_SupportingKafkaRedeliveryHandling()
    {
        var transactionId = $"txn-{Guid.NewGuid()}";
        var fraudCaseId = Guid.NewGuid();
        await using var writeContext = fixture.CreateReadWriteContext();
        var repository = new FraudCaseRepository(writeContext);

        (await repository.GetExistingFraudCaseAsync(transactionId, CancellationToken.None)).ShouldBeNull();

        await repository.SaveFraudCaseAsync(new FraudCaseResult
        {
            Id = fraudCaseId,
            TransactionId = transactionId,
            AccountId = "acc-1",
            CustomerId = "cust-1",
            Amount = 500m,
            Currency = "ZAR",
            Category = "gambling",
            Severity = FraudSeverity.High,
            OverallScore = 80,
            Status = FraudCaseStatus.Open,
            OccurredAt = DateTimeOffset.UtcNow,
            EvaluatedAt = DateTimeOffset.UtcNow,
            TriggeredRules = [new RuleEvaluationResult { RuleCode = "BLACKLISTED_MERCHANT", RuleName = "Blacklisted Merchant", Triggered = true, Score = 80, Reason = "on the list" }]
        }, correlationId: null, CancellationToken.None);

        var existing = await repository.GetExistingFraudCaseAsync(transactionId, CancellationToken.None);

        existing.ShouldNotBeNull();
        existing.Id.ShouldBe(fraudCaseId);
        existing.TriggeredRules.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Query_FiltersBySeverityAndPaginates()
    {
        var accountId = $"acc-{Guid.NewGuid()}";
        await using var writeContext = fixture.CreateReadWriteContext();
        var repository = new FraudCaseRepository(writeContext);

        foreach (var severity in new[] { FraudSeverity.Low, FraudSeverity.Medium, FraudSeverity.High, FraudSeverity.Critical })
        {
            await repository.SaveFraudCaseAsync(new FraudCaseResult
            {
                Id = Guid.NewGuid(),
                TransactionId = $"txn-{Guid.NewGuid()}",
                AccountId = accountId,
                CustomerId = "cust-1",
                Amount = 1000m,
                Currency = "ZAR",
                Category = "transfer",
                Severity = severity,
                OverallScore = 10,
                Status = FraudCaseStatus.Open,
                OccurredAt = DateTimeOffset.UtcNow,
                EvaluatedAt = DateTimeOffset.UtcNow,
                TriggeredRules = [new RuleEvaluationResult { RuleCode = "X", RuleName = "X", Triggered = true, Score = 10, Reason = "x" }]
            }, correlationId: null, CancellationToken.None);
        }

        await using var readContext = fixture.CreateReadOnlyContext();
        var queryRepository = new FraudCaseQueryRepository(readContext);

        var page = await queryRepository.QueryAsync(new FraudCaseQuery
        {
            AccountId = accountId,
            MinSeverity = FraudSeverity.High,
            Page = 1,
            PageSize = 10
        }, CancellationToken.None);

        page.TotalCount.ShouldBe(2);
        page.Items.ShouldAllBe(c => c.Severity >= FraudSeverity.High);
    }
}
