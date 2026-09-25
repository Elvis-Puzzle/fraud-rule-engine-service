using HealthChecks.NpgSql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace fraud_rule_engine_service.Configuration;

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ReadWrite")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:ReadWrite'.");

        services.AddHealthChecks()
            .AddNpgSql(new NpgSqlHealthCheckOptions(connectionString), name: "postgres", tags: ["ready"]);

        return services;
    }

    public static IEndpointRouteBuilder MapApplicationHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/liveness", new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapHealthChecks("/health/readiness", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        return endpoints;
    }
}
