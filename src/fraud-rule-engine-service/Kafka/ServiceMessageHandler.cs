using System.Text;
using Confluent.Kafka;
using fraud_rule_engine_service.Contracts.Events;

namespace fraud_rule_engine_service.Kafka;

/// <summary>
/// Dispatches a single consumed message to the <see cref="IServiceEventHandler"/> registered for
/// its `event-type` header. Keeping this separate from the consumer loop means the dispatch logic
/// can be unit tested without spinning up a real Kafka consumer.
/// </summary>
public sealed class ServiceMessageHandler(IEventHandlerRegistry registry, ILogger<ServiceMessageHandler> logger)
    : IServiceMessageHandler
{
    public async Task HandleAsync(ConsumeResult<string, byte[]> consumeResult, CancellationToken cancellationToken)
    {
        var eventType = GetHeader(consumeResult.Message.Headers, DomainEventHeaders.EventType);
        var correlationId = GetHeader(consumeResult.Message.Headers, DomainEventHeaders.CorrelationId);

        if (eventType is null)
        {
            logger.LogWarning(
                "Message on {Topic} partition {Partition} offset {Offset} has no '{Header}' header; skipping.",
                consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value, DomainEventHeaders.EventType);
            return;
        }

        if (!registry.TryGetHandler(eventType, out var handler))
        {
            logger.LogWarning("No handler registered for event type '{EventType}'; skipping.", eventType);
            return;
        }

        await handler.HandleAsync(consumeResult.Message.Value, correlationId, cancellationToken);
    }

    private static string? GetHeader(Headers? headers, string key)
    {
        if (headers is null || !headers.TryGetLastBytes(key, out var bytes))
        {
            return null;
        }

        return Encoding.UTF8.GetString(bytes);
    }
}
