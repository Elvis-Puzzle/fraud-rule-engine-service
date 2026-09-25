using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Persistence.Entities;

namespace fraud_rule_engine_service.Persistence;

internal static class EntityMappings
{
    public static Transaction ToDomain(this TransactionSnapshotEntity entity) => new()
    {
        TransactionId = entity.TransactionId,
        AccountId = entity.AccountId,
        CustomerId = entity.CustomerId,
        Amount = entity.Amount,
        Currency = entity.Currency,
        Category = entity.Category,
        Channel = entity.Channel,
        MerchantName = entity.MerchantName,
        MerchantCategoryCode = entity.MerchantCategoryCode,
        CounterpartyAccountId = entity.CounterpartyAccountId,
        IsNewPayee = entity.IsNewPayee,
        CountryCode = entity.CountryCode,
        DeviceId = entity.DeviceId,
        OccurredAt = entity.OccurredAt
    };

    public static TransactionSnapshotEntity ToSnapshotEntity(this Transaction transaction) => new()
    {
        Id = Guid.NewGuid(),
        TransactionId = transaction.TransactionId,
        AccountId = transaction.AccountId,
        CustomerId = transaction.CustomerId,
        Amount = transaction.Amount,
        Currency = transaction.Currency,
        Category = transaction.Category,
        Channel = transaction.Channel,
        MerchantName = transaction.MerchantName,
        MerchantCategoryCode = transaction.MerchantCategoryCode,
        CounterpartyAccountId = transaction.CounterpartyAccountId,
        IsNewPayee = transaction.IsNewPayee,
        CountryCode = transaction.CountryCode,
        DeviceId = transaction.DeviceId,
        OccurredAt = transaction.OccurredAt
    };

    public static FraudCaseResult ToDomain(this FraudCaseEntity entity) => new()
    {
        Id = entity.Id,
        TransactionId = entity.TransactionId,
        AccountId = entity.AccountId,
        CustomerId = entity.CustomerId,
        Amount = entity.Amount,
        Currency = entity.Currency,
        Category = entity.Category,
        Severity = entity.Severity,
        OverallScore = entity.OverallScore,
        Status = entity.Status,
        OccurredAt = entity.OccurredAt,
        EvaluatedAt = entity.EvaluatedAt,
        TriggeredRules = entity.TriggeredRules.Select(r => new RuleEvaluationResult
        {
            RuleCode = r.RuleCode,
            RuleName = r.RuleName,
            Triggered = true,
            Score = r.Score,
            Reason = r.Reason
        }).ToList()
    };
}
