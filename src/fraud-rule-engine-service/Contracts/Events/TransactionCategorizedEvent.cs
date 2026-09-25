using fraud_rule_engine_service.Domain.Models;

namespace fraud_rule_engine_service.Contracts.Events;

/// <summary>
/// The inbound event this service consumes: a transaction that has already been categorized
/// upstream (by the Transaction Aggregation API / categorization pipeline) and is now ready for
/// fraud-rule evaluation.
/// </summary>
public sealed record TransactionCategorizedEvent : DomainEvent
{
    public static readonly string EventType = "transaction-categorized-event";
    public static readonly string EventVersion = "1";

    public override DomainEventMetadata Metadata { get; init; } = new()
    {
        Type = EventType,
        Version = EventVersion,
        PublishedAt = DateTimeOffset.UtcNow
    };

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

    public Transaction ToTransaction() => new()
    {
        TransactionId = TransactionId,
        AccountId = AccountId,
        CustomerId = CustomerId,
        Amount = Amount,
        Currency = Currency,
        Category = Category,
        Channel = Channel,
        MerchantName = MerchantName,
        MerchantCategoryCode = MerchantCategoryCode,
        CounterpartyAccountId = CounterpartyAccountId,
        IsNewPayee = IsNewPayee,
        CountryCode = CountryCode,
        DeviceId = DeviceId,
        OccurredAt = OccurredAt
    };
}
