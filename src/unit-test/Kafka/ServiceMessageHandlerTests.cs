using System.Text;
using Confluent.Kafka;
using fraud_rule_engine_service.Contracts.Events;
using fraud_rule_engine_service.Kafka;
using Microsoft.Extensions.Logging;
using Moq;

namespace fraud_rule_engine_service.UnitTests.Kafka;

public class ServiceMessageHandlerTests
{
    private static ConsumeResult<string, byte[]> BuildConsumeResult(IReadOnlyDictionary<string, string>? headers = null)
    {
        var kafkaHeaders = new Headers();
        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                kafkaHeaders.Add(key, Encoding.UTF8.GetBytes(value));
            }
        }

        return new ConsumeResult<string, byte[]>
        {
            Topic = "transaction-categorized-events",
            Partition = new Partition(0),
            Offset = new Offset(0),
            Message = new Message<string, byte[]>
            {
                Key = "acc-1",
                Value = "{}"u8.ToArray(),
                Headers = kafkaHeaders
            }
        };
    }

    private static ServiceMessageHandler CreateHandler(IEventHandlerRegistry registry) =>
        new(registry, new LoggerFactory().CreateLogger<ServiceMessageHandler>());

    [Fact]
    public async Task HandleAsync_NoEventTypeHeader_DoesNotLookUpAHandler()
    {
        var registry = new Mock<IEventHandlerRegistry>();
        var handler = CreateHandler(registry.Object);

        await handler.HandleAsync(BuildConsumeResult(), CancellationToken.None);

        registry.Verify(r => r.TryGetHandler(It.IsAny<string>(), out It.Ref<IServiceEventHandler?>.IsAny), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UnregisteredEventType_DoesNotThrowAndDoesNotInvokeAnyHandler()
    {
        var eventHandler = new Mock<IServiceEventHandler>();
        var registry = new Mock<IEventHandlerRegistry>();
        IServiceEventHandler? unused = null;
        registry.Setup(r => r.TryGetHandler("unknown-event", out unused)).Returns(false);

        var handler = CreateHandler(registry.Object);
        var consumeResult = BuildConsumeResult(new Dictionary<string, string> { [DomainEventHeaders.EventType] = "unknown-event" });

        await handler.HandleAsync(consumeResult, CancellationToken.None);

        eventHandler.Verify(h => h.HandleAsync(It.IsAny<byte[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RegisteredEventType_InvokesTheMatchingHandlerWithPayloadAndCorrelationId()
    {
        var eventHandler = new Mock<IServiceEventHandler>();
        IServiceEventHandler? resolved = eventHandler.Object;
        var registry = new Mock<IEventHandlerRegistry>();
        registry.Setup(r => r.TryGetHandler(TransactionCategorizedEvent.EventType, out resolved)).Returns(true);

        var handler = CreateHandler(registry.Object);
        var consumeResult = BuildConsumeResult(new Dictionary<string, string>
        {
            [DomainEventHeaders.EventType] = TransactionCategorizedEvent.EventType,
            [DomainEventHeaders.CorrelationId] = "corr-123"
        });

        await handler.HandleAsync(consumeResult, CancellationToken.None);

        eventHandler.Verify(h => h.HandleAsync(consumeResult.Message.Value, "corr-123", It.IsAny<CancellationToken>()), Times.Once);
    }
}
