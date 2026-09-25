using System.Text.Json;
using fraud_rule_engine_service.Common.Json;
using fraud_rule_engine_service.Contracts.Events;
using fraud_rule_engine_service.Kafka.Options;
using fraud_rule_engine_service.Service;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Kafka.Handlers;

public sealed class TransactionCategorizedEventHandler(
    IFraudEvaluationService evaluationService,
    IKafkaProducerService producer,
    IValidator<TransactionCategorizedEvent> validator,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<TransactionCategorizedEventHandler> logger) : IServiceEventHandler
{
    public string EventType => TransactionCategorizedEvent.EventType;

    public async Task HandleAsync(byte[] payload, string? correlationId, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<TransactionCategorizedEvent>(payload, JsonSerialiserOptions.Default)
            ?? throw new InvalidOperationException($"Payload for '{EventType}' deserialized to null.");

        await validator.ValidateAndThrowAsync(@event, cancellationToken);

        var transaction = @event.ToTransaction();
        var fraudCase = await evaluationService.EvaluateAsync(transaction, correlationId, cancellationToken);

        if (fraudCase is null)
        {
            logger.LogDebug("Transaction {TransactionId} evaluated clean.", transaction.TransactionId);
            return;
        }

        // Republished on redelivery too — downstream consumers should be idempotent on FraudCaseId.
        var raisedEvent = new FraudCaseRaisedEvent
        {
            FraudCaseId = fraudCase.Id,
            TransactionId = fraudCase.TransactionId,
            AccountId = fraudCase.AccountId,
            CustomerId = fraudCase.CustomerId,
            Severity = fraudCase.Severity,
            OverallScore = fraudCase.OverallScore,
            TriggeredRuleCodes = fraudCase.TriggeredRules.Select(r => r.RuleCode).ToList()
        };

        var outgoingPayload = JsonSerializer.SerializeToUtf8Bytes(raisedEvent, JsonSerialiserOptions.Default);
        var headers = new Dictionary<string, string>
        {
            [DomainEventHeaders.EventType] = FraudCaseRaisedEvent.EventType
        };
        if (correlationId is not null)
        {
            headers[DomainEventHeaders.CorrelationId] = correlationId;
        }

        await producer.PublishAsync(
            kafkaOptions.Value.FraudCaseRaisedTopic, fraudCase.AccountId, outgoingPayload, headers, cancellationToken);
    }
}
