using fraud_rule_engine_service.Common;
using fraud_rule_engine_service.Common.Json;
using fraud_rule_engine_service.Common.Middleware;
using fraud_rule_engine_service.Configuration;
using fraud_rule_engine_service.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddApplicationLogging();

// Reuses the exact same JSON conventions (camelCase properties + enum names) as the Kafka message
// payloads, so the wire format looks identical whether you're curling the API or publishing an event.
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonSerialiserOptions.Default.PropertyNamingPolicy;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonSerialiserOptions.Default.DefaultIgnoreCondition;
        foreach (var converter in JsonSerialiserOptions.Default.Converters)
        {
            options.JsonSerializerOptions.Converters.Add(converter);
        }
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocumentation();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IApplicationContext, ApplicationContext>();

builder.Services.AddApplicationValidation();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddFraudRuleEngine(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddApplicationHealthChecks(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// `-m` / `--m=true` runs pending EF Core migrations against the write connection then exits,
// so migrations ship inside the app image rather than as a separate pipeline step.
if (args.Contains("-m") || args.Contains("--m=true"))
{
    using var migrationHost = builder.Build();
    using var scope = migrationHost.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationReadWriteContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapApplicationHealthChecks();

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
