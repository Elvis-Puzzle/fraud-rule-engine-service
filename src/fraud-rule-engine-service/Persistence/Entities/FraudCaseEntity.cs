using fraud_rule_engine_service.Domain.Models;

namespace fraud_rule_engine_service.Persistence.Entities;

public sealed class FraudCaseEntity
{
    public Guid Id { get; set; }
    public required string TransactionId { get; set; }
    public required string AccountId { get; set; }
    public required string CustomerId { get; set; }
    public required decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string Category { get; set; }
    public required FraudSeverity Severity { get; set; }
    public required int OverallScore { get; set; }
    public required FraudCaseStatus Status { get; set; }
    public string? CorrelationId { get; set; }
    public required DateTimeOffset OccurredAt { get; set; }
    public required DateTimeOffset EvaluatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<FraudCaseTriggeredRuleEntity> TriggeredRules { get; set; } = [];
}
