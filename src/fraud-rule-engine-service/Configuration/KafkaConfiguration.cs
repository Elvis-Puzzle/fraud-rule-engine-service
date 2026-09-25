using Confluent.Kafka;
using fraud_rule_engine_service.Kafka;
using fraud_rule_engine_service.Kafka.Handlers;
using fraud_rule_engine_service.Kafka.Options;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Configuration;

public static class KafkaConfiguration
{
    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // The producer owns one long-lived, thread-safe Confluent.Kafka client, so it's a singleton.
        services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

        // Built here (rather than inside KafkaConsumerWorker itself) so the worker only ever
        // depends on the IConsumer<> interface — that's what lets a test hand it a mocked
        // consumer instead of a real broker connection.
        services.AddSingleton<IConsumer<string, byte[]>>(sp =>
        {
            var kafkaOptions = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
            var config = new ConsumerConfig
            {
                BootstrapServers = kafkaOptions.BootstrapServers,
                GroupId = kafkaOptions.ConsumerGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
            return new ConsumerBuilder<string, byte[]>(config).Build();
        });

        // Handlers and the registry ultimately depend on scoped EF Core contexts, so they're scoped
        // too — the consumer worker resolves a fresh scope per message via IServiceScopeFactory.
        services.AddScoped<IServiceEventHandler, TransactionCategorizedEventHandler>();
        services.AddScoped<IEventHandlerRegistry, EventHandlerRegistry>();
        services.AddScoped<IServiceMessageHandler, ServiceMessageHandler>();
        services.AddHostedService<KafkaConsumerWorker>();

        return services;
    }
}
