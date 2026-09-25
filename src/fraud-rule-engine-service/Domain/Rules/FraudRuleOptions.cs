using System.ComponentModel.DataAnnotations;

namespace fraud_rule_engine_service.Domain.Rules;

/// <summary>
/// Thresholds for every rule, bound from the "FraudRules" configuration section so risk appetite
/// can be tuned per environment without a code change or redeploy.
/// </summary>
public sealed class FraudRuleOptions
{
    public const string SectionName = "FraudRules";

    [Range(1, int.MaxValue)]
    public decimal HighValueThreshold { get; init; } = 50_000m;

    [Range(1, 1440)]
    public int VelocityWindowMinutes { get; init; } = 10;

    [Range(1, 1000)]
    public int VelocityMaxTransactions { get; init; } = 5;

    [Range(1, int.MaxValue)]
    public decimal StructuringSubThreshold { get; init; } = 10_000m;

    [Range(1, int.MaxValue)]
    public decimal StructuringAggregateThreshold { get; init; } = 25_000m;

    [Range(1, 1440)]
    public int StructuringWindowMinutes { get; init; } = 60;

    [Range(1, 1440)]
    public int ImpossibleTravelWindowMinutes { get; init; } = 120;

    [Range(0, 23)]
    public int UnusualHourStartUtc { get; init; } = 0;

    [Range(0, 23)]
    public int UnusualHourEndUtc { get; init; } = 4;

    [Range(1, int.MaxValue)]
    public decimal UnusualHourAmountThreshold { get; init; } = 5_000m;

    [Range(1, int.MaxValue)]
    public decimal NewPayeeAmountThreshold { get; init; } = 15_000m;

    public string[] BlacklistedMerchantCategoryCodes { get; init; } = [];

    public string[] BlacklistedMerchantNames { get; init; } = [];

    [Range(1, 1440)]
    public int DuplicateWindowMinutes { get; init; } = 2;

    [Range(1, 1440)]
    public int HistoryLookbackMinutes { get; init; } = 1440;
}
