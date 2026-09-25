namespace fraud_rule_engine_service.Common;

/// <summary>Scoped, per-request ambient context — currently just the inbound correlation id.</summary>
public interface IApplicationContext
{
    string? CorrelationId { get; set; }
}

public sealed class ApplicationContext : IApplicationContext
{
    public string? CorrelationId { get; set; }
}
