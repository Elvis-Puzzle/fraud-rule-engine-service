using FluentValidation;

namespace fraud_rule_engine_service.Configuration;

public static class ValidationConfiguration
{
    public static IServiceCollection AddApplicationValidation(this IServiceCollection services) =>
        services.AddValidatorsFromAssemblyContaining<Program>();
}
