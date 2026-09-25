namespace fraud_rule_engine_service.Common;

/// <summary>Role names expected in the JWT's `role` claim. Kept as constants so a typo can't silently create a new, unintended role.</summary>
public static class Roles
{
    /// <summary>Allowed to investigate and transition the status of a fraud case.</summary>
    public const string FraudAnalyst = "FraudAnalyst";
}
