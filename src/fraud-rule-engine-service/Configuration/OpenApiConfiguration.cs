using Microsoft.OpenApi.Models;

namespace fraud_rule_engine_service.Configuration;

public static class OpenApiConfiguration
{
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Fraud Rule Engine Service",
                Version = "v1",
                Description = "Evaluates categorized transaction events against a configurable set of fraud rules and exposes retrieval of the resulting fraud cases."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT bearer token issued by the internal IdP."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    []
                }
            });
        });

        return services;
    }
}
