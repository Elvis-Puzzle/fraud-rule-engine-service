namespace fraud_rule_engine_service.Persistence.Entities;

public sealed class FraudCaseTriggeredRuleEntity
{
    public Guid Id { get; set; }
    public Guid FraudCaseId { get; set; }
    public required string RuleCode { get; set; }
    public required string RuleName { get; set; }
    public required int Score { get; set; }
    public required string Reason { get; set; }

    public FraudCaseEntity? FraudCase { get; set; }
}
