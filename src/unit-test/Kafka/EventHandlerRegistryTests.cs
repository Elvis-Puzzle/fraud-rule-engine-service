using fraud_rule_engine_service.Kafka;
using Moq;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Kafka;

public class EventHandlerRegistryTests
{
    [Fact]
    public void TryGetHandler_RegisteredEventType_ReturnsHandler()
    {
        var handler = Mock.Of<IServiceEventHandler>(h => h.EventType == "transaction-categorized-event");
        var registry = new EventHandlerRegistry([handler]);

        var found = registry.TryGetHandler("transaction-categorized-event", out var resolved);

        found.ShouldBeTrue();
        resolved.ShouldBe(handler);
    }

    [Fact]
    public void TryGetHandler_UnregisteredEventType_ReturnsFalse()
    {
        var registry = new EventHandlerRegistry([]);

        var found = registry.TryGetHandler("unknown-event", out var resolved);

        found.ShouldBeFalse();
        resolved.ShouldBeNull();
    }

    [Fact]
    public void Constructor_DuplicateEventTypeRegistration_Throws()
    {
        var handlerA = Mock.Of<IServiceEventHandler>(h => h.EventType == "duplicate-event");
        var handlerB = Mock.Of<IServiceEventHandler>(h => h.EventType == "duplicate-event");

        Should.Throw<InvalidOperationException>(() => new EventHandlerRegistry([handlerA, handlerB]));
    }

    [Fact]
    public void TryGetHandler_EventTypeLookupIsCaseInsensitive()
    {
        var handler = Mock.Of<IServiceEventHandler>(h => h.EventType == "Transaction-Categorized-Event");
        var registry = new EventHandlerRegistry([handler]);

        var found = registry.TryGetHandler("transaction-categorized-event", out _);

        found.ShouldBeTrue();
    }
}
