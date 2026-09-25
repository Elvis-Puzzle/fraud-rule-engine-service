using System.Text.Json;
using fraud_rule_engine_service.Common.Json;
using fraud_rule_engine_service.Contracts.Events;
using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Kafka;
using fraud_rule_engine_service.Kafka.Handlers;
using fraud_rule_engine_service.Kafka.Options;
using fraud_rule_engine_service.Service;
using fraud_rule_engine_service.Validators;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Kafka;

public class TransactionCategorizedEventHandlerTests
{
    private readonly Mock<IFraudEvaluationService> _evaluationService = new();
    private readonly Mock<IKafkaProducerService> _producer = new();

    private TransactionCategorizedEventHandler CreateHandler() => new(
        _evaluationService.Object,
        _producer.Object,
        new TransactionCategorizedEventValidator(),
        Options.Create(new KafkaOptions()),
        new LoggerFactory().CreateLogger<TransactionCategorizedEventHandler>());

    private static byte[] Serialize(TransactionCategorizedEvent @event) =>
        JsonSerializer.SerializeToUtf8Bytes(@event, JsonSerialiserOptions.Default);

    private static TransactionCategorizedEvent ValidEvent(string transactionId = "txn-1") => new()
    {
        TransactionId = transactionId,
        AccountId = "acc-1",
        CustomerId = "cust-1",
        Amount = 500m,
        Currency = "ZAR",
        Category = "groceries",
        Channel = TransactionChannel.Pos,
        CountryCode = "ZA",
        OccurredAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task HandleAsync_InvalidPayload_ThrowsAndNeverEvaluates()
    {
        var invalidEvent = ValidEvent() with { Amount = -1 };
        var handler = CreateHandler();

        await Should.ThrowAsync<ValidationException>(() =>
            handler.HandleAsync(Serialize(invalidEvent), correlationId: null, CancellationToken.None));

        _evaluationService.Verify(
            s => s.EvaluateAsync(It.IsAny<Transaction>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EvaluatesClean_NeverPublishesToTheOutgoingTopic()
    {
        _evaluationService
            .Setup(s => s.EvaluateAsync(It.IsAny<Transaction>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FraudCaseResult?)null);

        var handler = CreateHandler();
        await handler.HandleAsync(Serialize(ValidEvent()), correlationId: null, CancellationToken.None);

        _producer.Verify(
            p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_FraudCaseRaised_PublishesNotificationWithCorrelationIdHeader()
    {
        var fraudCase = new FraudCaseResult
        {
            Id = Guid.NewGuid(),
            TransactionId = "txn-1",
            AccountId = "acc-1",
            CustomerId = "cust-1",
            Amount = 500m,
            Currency = "ZAR",
            Category = "groceries",
            Severity = FraudSeverity.High,
            OverallScore = 80,
            Status = FraudCaseStatus.Open,
            OccurredAt = DateTimeOffset.UtcNow,
            EvaluatedAt = DateTimeOffset.UtcNow,
            TriggeredRules = [new RuleEvaluationResult { RuleCode = "HIGH_VALUE", RuleName = "High Value", Triggered = true, Score = 80, Reason = "too high" }]
        };
        _evaluationService
            .Setup(s => s.EvaluateAsync(It.IsAny<Transaction>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fraudCase);

        IReadOnlyDictionary<string, string>? capturedHeaders = null;
        _producer
            .Setup(p => p.PublishAsync(
                "fraud-case-raised-events", "acc-1", It.IsAny<byte[]>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, byte[], IReadOnlyDictionary<string, string>?, CancellationToken>(
                (_, _, _, headers, _) => capturedHeaders = headers)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        await handler.HandleAsync(Serialize(ValidEvent()), correlationId: "corr-1", CancellationToken.None);

        _producer.Verify(
            p => p.PublishAsync("fraud-case-raised-events", "acc-1", It.IsAny<byte[]>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        capturedHeaders.ShouldNotBeNull();
        capturedHeaders[DomainEventHeaders.CorrelationId].ShouldBe("corr-1");
        capturedHeaders[DomainEventHeaders.EventType].ShouldBe(FraudCaseRaisedEvent.EventType);
    }
}
