using fraud_rule_engine_service.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace fraud_rule_engine_service.IntegrationTests.Persistence;

/// <summary>
/// Spins up a real Postgres instance via Testcontainers and applies EF Core migrations, so
/// repository tests exercise the same SQL the service runs in production rather than an
/// in-memory provider that hides provider-specific behaviour (snake_case columns, precision, etc).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("fraud_rule_engine")
        .WithUsername("fraud_rule_engine")
        .WithPassword("fraud_rule_engine")
        .Build();

    public ApplicationReadWriteContext CreateReadWriteContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationReadWriteContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        return new ApplicationReadWriteContext(options);
    }

    public ApplicationReadOnlyContext CreateReadOnlyContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationReadOnlyContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        return new ApplicationReadOnlyContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        using var context = CreateReadWriteContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
