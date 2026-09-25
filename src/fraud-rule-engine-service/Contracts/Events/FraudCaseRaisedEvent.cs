using fraud_rule_engine_service.Domain.Models;

namespace fraud_rule_engine_service.Contracts.Events;

/// <summary>Published whenever a fraud case is raised, so downstream systems (e.g. case management, notifications) can react.</summary>
public sealed record FraudCaseRaisedEvent : DomainEvent
{
    public static readonly string EventType = "fraud-case-raised-event";
    public static readonly string EventVersion = "1";

    public override DomainEventMetadata Metadata { get; init; } = new()
    {
        Type = EventType,
        Version = EventVersion,
        PublishedAt = DateTimeOffset.UtcNow
    };

    public required Guid FraudCaseId { get; init; }
    public required string TransactionId { get; init; }
    public required string AccountId { get; init; }
    public required string CustomerId { get; init; }
    public required FraudSeverity Severity { get; init; }
    public required int OverallScore { get; init; }
    public required IReadOnlyList<string> TriggeredRuleCodes { get; init; }
}
