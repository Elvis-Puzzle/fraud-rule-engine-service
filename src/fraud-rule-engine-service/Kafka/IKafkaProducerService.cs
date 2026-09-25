namespace fraud_rule_engine_service.Kafka;

public interface IKafkaProducerService
{
    Task PublishAsync(
        string topic,
        string key,
        byte[] value,
        IReadOnlyDictionary<string, string>? headers,
        CancellationToken cancellationToken);
}
