namespace fraud_rule_engine_service.Domain.Models;

public enum TransactionChannel
{
    Pos,
    Atm,
    Eft,
    InstantPayment,
    Online,
    CardNotPresent
}

public enum FraudSeverity
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum FraudCaseStatus
{
    Open,
    UnderReview,
    Confirmed,
    FalsePositive
}
