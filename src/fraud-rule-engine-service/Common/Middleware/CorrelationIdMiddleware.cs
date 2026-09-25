using Serilog.Context;

namespace fraud_rule_engine_service.Common.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext httpContext, IApplicationContext applicationContext)
    {
        var correlationId = httpContext.Request.Headers.TryGetValue(HeaderName, out var value)
            ? value.ToString()
            : Guid.NewGuid().ToString();

        applicationContext.CorrelationId = correlationId;
        httpContext.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(httpContext);
        }
    }
}
