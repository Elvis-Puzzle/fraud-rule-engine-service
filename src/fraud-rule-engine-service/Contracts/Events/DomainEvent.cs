namespace fraud_rule_engine_service.Contracts.Events;

/// <summary>Base shape every Kafka event in this service follows: a type/version/timestamp envelope.</summary>
public abstract record DomainEvent
{
    public abstract DomainEventMetadata Metadata { get; init; }
}

public sealed record DomainEventMetadata
{
    public required string Type { get; init; }
    public required string Version { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
    public string? CorrelationId { get; init; }
}
