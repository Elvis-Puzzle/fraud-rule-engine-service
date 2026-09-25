using Serilog;

namespace fraud_rule_engine_service.Configuration;

/// <summary>
/// Console-only Serilog setup. A production deployment would add a centralized log sink here
/// (enriched with pod/host metadata) alongside the console output.
/// </summary>
public static class LoggingConfiguration
{
    public static IHostBuilder AddApplicationLogging(this IHostBuilder hostBuilder) =>
        hostBuilder.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", context.HostingEnvironment.ApplicationName)
            .WriteTo.Console());
}
