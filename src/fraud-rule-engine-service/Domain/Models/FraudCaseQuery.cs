namespace fraud_rule_engine_service.Domain.Models;

public sealed record FraudCaseQuery
{
    public string? AccountId { get; init; }
    public string? CustomerId { get; init; }
    public FraudSeverity? MinSeverity { get; init; }
    public FraudCaseStatus? Status { get; init; }
    public string? RuleCode { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record FraudCaseSummary
{
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }
    public required int TotalCases { get; init; }
    public required IReadOnlyDictionary<FraudSeverity, int> CasesBySeverity { get; init; }
    public required IReadOnlyDictionary<string, int> CasesByRule { get; init; }
}
