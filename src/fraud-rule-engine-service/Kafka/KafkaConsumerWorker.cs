using Confluent.Kafka;
using fraud_rule_engine_service.Kafka.Options;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Kafka;

/// <summary>
/// Background consumer for the incoming transaction-categorized topic. A handler failure routes
/// the message to the DLQ instead of blocking the partition; the offset commits only once the
/// message is handled or safely routed to the DLQ.
/// </summary>
public sealed class KafkaConsumerWorker(
    IConsumer<string, byte[]> consumer,
    IOptions<KafkaOptions> options,
    IServiceScopeFactory scopeFactory,
    IKafkaProducerService producer,
    ILogger<KafkaConsumerWorker> logger) : BackgroundService
{
    private readonly KafkaOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(_options.IncomingTopic);
        logger.LogInformation("Subscribed to {Topic} as consumer group {GroupId}.", _options.IncomingTopic, _options.ConsumerGroupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
                if (consumeResult is null || consumeResult.IsPartitionEOF)
                {
                    continue;
                }

                var safeToCommit = await ProcessAsync(consumeResult, stoppingToken);
                if (safeToCommit)
                {
                    consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error on {Topic}.", _options.IncomingTopic);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        consumer.Close();
    }

    /// <summary>Returns true if the offset is safe to commit — either the message was handled, or it was safely routed to the DLQ.</summary>
    private async Task<bool> ProcessAsync(ConsumeResult<string, byte[]> consumeResult, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var messageHandler = scope.ServiceProvider.GetRequiredService<IServiceMessageHandler>();
            await messageHandler.HandleAsync(consumeResult, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, "Failed to process message on {Topic} partition {Partition} offset {Offset}; routing to DLQ.",
                consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);

            return await TrySendToDlqAsync(consumeResult, ex, cancellationToken);
        }
    }

    private async Task<bool> TrySendToDlqAsync(ConsumeResult<string, byte[]> consumeResult, Exception exception, CancellationToken cancellationToken)
    {
        try
        {
            var headers = new Dictionary<string, string> { ["error"] = exception.Message };
            foreach (var header in consumeResult.Message.Headers)
            {
                headers[header.Key] = System.Text.Encoding.UTF8.GetString(header.GetValueBytes());
            }

            await producer.PublishAsync(
                _options.DlqTopic, consumeResult.Message.Key, consumeResult.Message.Value, headers, cancellationToken);
            return true;
        }
        catch (Exception dlqException)
        {
            // Must not rethrow: an exception escaping a BackgroundService's ExecuteAsync kills the
            // whole host. Leave the offset uncommitted instead — a restart retries both steps.
            logger.LogCritical(
                dlqException,
                "Failed to publish to DLQ topic {DlqTopic} for message on {Topic} partition {Partition} offset {Offset}; leaving the offset uncommitted so it is retried.",
                _options.DlqTopic, consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
            return false;
        }
    }
}
