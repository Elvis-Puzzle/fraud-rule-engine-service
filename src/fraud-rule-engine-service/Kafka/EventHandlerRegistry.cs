using System.Diagnostics.CodeAnalysis;

namespace fraud_rule_engine_service.Kafka;

public sealed class EventHandlerRegistry : IEventHandlerRegistry
{
    private readonly Dictionary<string, IServiceEventHandler> _handlers;

    public EventHandlerRegistry(IEnumerable<IServiceEventHandler> handlers)
    {
        _handlers = new Dictionary<string, IServiceEventHandler>(StringComparer.OrdinalIgnoreCase);
        foreach (var handler in handlers)
        {
            if (!_handlers.TryAdd(handler.EventType, handler))
            {
                throw new InvalidOperationException(
                    $"Duplicate event handler registration for event type '{handler.EventType}'.");
            }
        }
    }

    public bool TryGetHandler(string eventType, [NotNullWhen(true)] out IServiceEventHandler? handler) =>
        _handlers.TryGetValue(eventType, out handler);
}
