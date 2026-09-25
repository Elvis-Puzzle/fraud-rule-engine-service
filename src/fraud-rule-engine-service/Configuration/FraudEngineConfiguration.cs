using fraud_rule_engine_service.Domain.Rules;
using fraud_rule_engine_service.Service;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Configuration;

public static class FraudEngineConfiguration
{
    public static IServiceCollection AddFraudRuleEngine(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<FraudRuleOptions>()
            .Bind(configuration.GetSection(FraudRuleOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<FraudRuleOptions>, FraudRuleOptionsValidator>();

        services.AddSingleton<IFraudRule, HighValueTransactionRule>();
        services.AddSingleton<IFraudRule, VelocityRule>();
        services.AddSingleton<IFraudRule, StructuringRule>();
        services.AddSingleton<IFraudRule, ImpossibleTravelRule>();
        services.AddSingleton<IFraudRule, UnusualHourRule>();
        services.AddSingleton<IFraudRule, NewPayeeLargeTransferRule>();
        services.AddSingleton<IFraudRule, BlacklistedMerchantRule>();
        services.AddSingleton<IFraudRule, DuplicateTransactionRule>();

        services.AddScoped<IFraudEvaluationService, FraudEvaluationService>();
        services.AddScoped<IFraudCaseQueryService, FraudCaseQueryService>();

        return services;
    }
}
