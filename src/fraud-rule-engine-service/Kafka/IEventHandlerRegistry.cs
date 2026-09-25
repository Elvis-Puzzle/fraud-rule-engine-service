using System.Diagnostics.CodeAnalysis;

namespace fraud_rule_engine_service.Kafka;

public interface IEventHandlerRegistry
{
    bool TryGetHandler(string eventType, [NotNullWhen(true)] out IServiceEventHandler? handler);
}
