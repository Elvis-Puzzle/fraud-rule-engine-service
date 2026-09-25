using System.ComponentModel.DataAnnotations;

namespace fraud_rule_engine_service.Kafka.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    [Required]
    public string BootstrapServers { get; init; } = "localhost:9092";

    [Required]
    public string ConsumerGroupId { get; init; } = "fraud-rule-engine-service";

    [Required]
    public string IncomingTopic { get; init; } = "transaction-categorized-events";

    [Required]
    public string DlqTopic { get; init; } = "transaction-categorized-events.dlq";

    [Required]
    public string FraudCaseRaisedTopic { get; init; } = "fraud-case-raised-events";
}
