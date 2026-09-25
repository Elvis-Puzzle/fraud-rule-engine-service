using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace fraud_rule_engine_service.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` run without a fully configured host or secrets — points at a
/// throwaway local connection string, never used at runtime.
/// </summary>
public sealed class DesignTimeApplicationDbContext : IDesignTimeDbContextFactory<ApplicationReadWriteContext>
{
    public ApplicationReadWriteContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationReadWriteContext>();
        optionsBuilder
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=fraud_rule_engine;Username=fraud_rule_engine;Password=fraud_rule_engine",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "fraud_rule_engine"))
            .UseSnakeCaseNamingConvention();
        return new ApplicationReadWriteContext(optionsBuilder.Options);
    }
}
