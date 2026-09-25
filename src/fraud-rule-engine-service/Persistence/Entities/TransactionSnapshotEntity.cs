using fraud_rule_engine_service.Domain.Models;

namespace fraud_rule_engine_service.Persistence.Entities;

/// <summary>
/// Durable record of every transaction that has passed through evaluation, kept for the
/// lookback window rules (velocity, structuring, impossible travel, duplicate) need to query.
/// </summary>
public sealed class TransactionSnapshotEntity
{
    public Guid Id { get; set; }
    public required string TransactionId { get; set; }
    public required string AccountId { get; set; }
    public required string CustomerId { get; set; }
    public required decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string Category { get; set; }
    public required TransactionChannel Channel { get; set; }
    public string? MerchantName { get; set; }
    public string? MerchantCategoryCode { get; set; }
    public string? CounterpartyAccountId { get; set; }
    public bool IsNewPayee { get; set; }
    public required string CountryCode { get; set; }
    public string? DeviceId { get; set; }
    public required DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
