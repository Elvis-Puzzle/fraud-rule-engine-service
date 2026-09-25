using fraud_rule_engine_service.Domain.Models;

namespace fraud_rule_engine_service.UnitTests.TestHelpers;

internal static class TransactionFactory
{
    public static Transaction Default(
        string transactionId = "txn-1",
        string accountId = "acc-1",
        string customerId = "cust-1",
        decimal amount = 100m,
        string currency = "ZAR",
        string category = "groceries",
        TransactionChannel channel = TransactionChannel.Pos,
        string? merchantName = "Local Store",
        string? merchantCategoryCode = "5411",
        string? counterpartyAccountId = null,
        bool isNewPayee = false,
        string countryCode = "ZA",
        string? deviceId = "device-1",
        DateTimeOffset? occurredAt = null) => new()
    {
        TransactionId = transactionId,
        AccountId = accountId,
        CustomerId = customerId,
        Amount = amount,
        Currency = currency,
        Category = category,
        Channel = channel,
        MerchantName = merchantName,
        MerchantCategoryCode = merchantCategoryCode,
        CounterpartyAccountId = counterpartyAccountId,
        IsNewPayee = isNewPayee,
        CountryCode = countryCode,
        DeviceId = deviceId,
        OccurredAt = occurredAt ?? new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)
    };
}
