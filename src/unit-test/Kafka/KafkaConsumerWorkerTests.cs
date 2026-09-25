using System.Reflection;
using Confluent.Kafka;
using fraud_rule_engine_service.Kafka;
using fraud_rule_engine_service.Kafka.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace fraud_rule_engine_service.UnitTests.Kafka;

public class KafkaConsumerWorkerTests
{
    private readonly Mock<IConsumer<string, byte[]>> _consumer = new();
    private readonly Mock<IServiceMessageHandler> _messageHandler = new();
    private readonly Mock<IKafkaProducerService> _producer = new();

    private static ConsumeResult<string, byte[]> BuildConsumeResult() => new()
    {
        Topic = "transaction-categorized-events",
        Partition = new Partition(0),
        Offset = new Offset(0),
        Message = new Message<string, byte[]> { Key = "acc-1", Value = "{}"u8.ToArray(), Headers = new Headers() }
    };

    /// <summary>ExecuteAsync loops forever, so the mock hands back one message then throws OperationCanceledException to break the loop.</summary>
    private void SetUpConsumeOnceThenStop(ConsumeResult<string, byte[]> consumeResult)
    {
        var callCount = 0;
        _consumer.Setup(c => c.Consume(It.IsAny<TimeSpan>())).Returns(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                return consumeResult;
            }

            throw new OperationCanceledException();
        });
    }

    private KafkaConsumerWorker CreateWorker()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IServiceMessageHandler))).Returns(_messageHandler.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        return new KafkaConsumerWorker(
            _consumer.Object,
            Options.Create(new KafkaOptions()),
            scopeFactory.Object,
            _producer.Object,
            new LoggerFactory().CreateLogger<KafkaConsumerWorker>());
    }

    private static async Task RunExecuteAsync(KafkaConsumerWorker worker)
    {
        var method = typeof(KafkaConsumerWorker).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(worker, [CancellationToken.None])!;
    }

    [Fact]
    public async Task ExecuteAsync_MessageHandledSuccessfully_CommitsAndNeverTouchesTheDlq()
    {
        var consumeResult = BuildConsumeResult();
        SetUpConsumeOnceThenStop(consumeResult);
        _messageHandler.Setup(h => h.HandleAsync(consumeResult, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await RunExecuteAsync(CreateWorker());

        _consumer.Verify(c => c.Commit(consumeResult), Times.Once);
        _producer.Verify(
            p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_HandlerThrows_RoutesToDlqAndStillCommits()
    {
        var consumeResult = BuildConsumeResult();
        SetUpConsumeOnceThenStop(consumeResult);
        _messageHandler.Setup(h => h.HandleAsync(consumeResult, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        _producer
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await RunExecuteAsync(CreateWorker());

        _producer.Verify(
            p => p.PublishAsync("transaction-categorized-events.dlq", "acc-1", It.IsAny<byte[]>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _consumer.Verify(c => c.Commit(consumeResult), Times.Once);
    }

    /// <summary>A DLQ-publish failure must not crash the host — the task must complete, not throw.</summary>
    [Fact]
    public async Task ExecuteAsync_HandlerThrowsAndDlqPublishAlsoThrows_DoesNotCrashAndDoesNotCommit()
    {
        var consumeResult = BuildConsumeResult();
        SetUpConsumeOnceThenStop(consumeResult);
        _messageHandler.Setup(h => h.HandleAsync(consumeResult, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        _producer
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KafkaException(new Error(ErrorCode.Local_Transport, "broker unreachable")));

        await RunExecuteAsync(CreateWorker()); // must complete without throwing

        _consumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, byte[]>>()), Times.Never);
    }
}
