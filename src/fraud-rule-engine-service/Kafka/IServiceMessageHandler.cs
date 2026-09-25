using Confluent.Kafka;

namespace fraud_rule_engine_service.Kafka;

public interface IServiceMessageHandler
{
    Task HandleAsync(ConsumeResult<string, byte[]> consumeResult, CancellationToken cancellationToken);
}
