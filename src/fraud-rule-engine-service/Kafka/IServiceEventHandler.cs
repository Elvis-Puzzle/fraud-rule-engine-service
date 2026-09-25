namespace fraud_rule_engine_service.Kafka;

/// <summary>One handler per inbound event type, looked up by the `event-type` header via <see cref="IEventHandlerRegistry"/>.</summary>
public interface IServiceEventHandler
{
    string EventType { get; }

    Task HandleAsync(byte[] payload, string? correlationId, CancellationToken cancellationToken);
}
