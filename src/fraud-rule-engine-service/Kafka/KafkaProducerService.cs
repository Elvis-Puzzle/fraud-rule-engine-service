using Confluent.Kafka;
using fraud_rule_engine_service.Kafka.Options;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Kafka;

/// <summary>Thin wrapper around a single, shared Confluent.Kafka producer instance.</summary>
public sealed class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IOptions<KafkaOptions> options, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All
        };
        _producer = new ProducerBuilder<string, byte[]>(config).Build();
    }

    public async Task PublishAsync(
        string topic,
        string key,
        byte[] value,
        IReadOnlyDictionary<string, string>? headers,
        CancellationToken cancellationToken)
    {
        var message = new Message<string, byte[]>
        {
            Key = key,
            Value = value,
            Headers = ToKafkaHeaders(headers)
        };

        var result = await _producer.ProduceAsync(topic, message, cancellationToken);
        _logger.LogDebug(
            "Published message to {Topic} partition {Partition} offset {Offset}.",
            result.Topic, result.Partition.Value, result.Offset.Value);
    }

    private static Headers? ToKafkaHeaders(IReadOnlyDictionary<string, string>? headers)
    {
        if (headers is null || headers.Count == 0)
        {
            return null;
        }

        var kafkaHeaders = new Headers();
        foreach (var (key, value) in headers)
        {
            kafkaHeaders.Add(key, System.Text.Encoding.UTF8.GetBytes(value));
        }

        return kafkaHeaders;
    }

    public void Dispose() => _producer.Dispose();
}
