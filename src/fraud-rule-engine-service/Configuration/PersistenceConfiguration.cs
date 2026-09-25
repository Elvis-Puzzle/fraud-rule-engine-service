using fraud_rule_engine_service.Persistence;
using fraud_rule_engine_service.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace fraud_rule_engine_service.Configuration;

public static class PersistenceConfiguration
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var readWriteConnectionString = configuration.GetConnectionString("ReadWrite")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:ReadWrite'.");
        var readOnlyConnectionString = configuration.GetConnectionString("ReadOnly")
            ?? readWriteConnectionString;

        services.AddDbContextPool<ApplicationReadWriteContext>(options => options
            .UseNpgsql(readWriteConnectionString, npgsql => npgsql
                .MigrationsHistoryTable("__ef_migrations_history", "fraud_rule_engine"))
            .UseSnakeCaseNamingConvention());

        services.AddDbContextPool<ApplicationReadOnlyContext>(options => options
            .UseNpgsql(readOnlyConnectionString)
            .UseSnakeCaseNamingConvention()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IFraudCaseRepository, FraudCaseRepository>();
        services.AddScoped<IFraudCaseQueryRepository, FraudCaseQueryRepository>();

        return services;
    }
}
