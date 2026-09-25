namespace fraud_rule_engine_service.Domain.Models;

/// <summary>
/// A single categorized transaction as received from the upstream transaction-categorization event stream.
/// </summary>
public sealed record Transaction
{
    public required string TransactionId { get; init; }
    public required string AccountId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string Category { get; init; }
    public required TransactionChannel Channel { get; init; }
    public string? MerchantName { get; init; }
    public string? MerchantCategoryCode { get; init; }
    public string? CounterpartyAccountId { get; init; }
    public bool IsNewPayee { get; init; }
    public required string CountryCode { get; init; }
    public string? DeviceId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>
/// Everything a rule needs to evaluate a transaction: the transaction itself, plus a bounded
/// window of the account's recent history, pulled once per evaluation and shared across all rules
/// so no rule has to hit the store independently.
/// </summary>
public sealed record TransactionEvaluationContext
{
    public required Transaction Current { get; init; }
    public required IReadOnlyList<Transaction> RecentHistory { get; init; }
}

public sealed record RuleEvaluationResult
{
    public required string RuleCode { get; init; }
    public required string RuleName { get; init; }
    public required bool Triggered { get; init; }
    public int Score { get; init; }
    public string? Reason { get; init; }

    public static RuleEvaluationResult NotTriggered(string code, string name) => new()
    {
        RuleCode = code, RuleName = name, Triggered = false, Score = 0
    };

    public static RuleEvaluationResult Trigger(string code, string name, int score, string reason) => new()
    {
        RuleCode = code, RuleName = name, Triggered = true, Score = score, Reason = reason
    };
}

public sealed record FraudCaseResult
{
    public required Guid Id { get; init; }
    public required string TransactionId { get; init; }
    public required string AccountId { get; init; }
    public required string CustomerId { get; init; }
    public required string Category { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required FraudSeverity Severity { get; init; }
    public required int OverallScore { get; init; }
    public required FraudCaseStatus Status { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required DateTimeOffset EvaluatedAt { get; init; }
    public required IReadOnlyList<RuleEvaluationResult> TriggeredRules { get; init; }
}
