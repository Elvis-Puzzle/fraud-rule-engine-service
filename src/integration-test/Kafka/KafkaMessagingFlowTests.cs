using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using fraud_rule_engine_service.Common.Json;
using fraud_rule_engine_service.Configuration;
using fraud_rule_engine_service.Contracts.Events;
using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.IntegrationTests.Persistence;
using fraud_rule_engine_service.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using KafkaConsumerWorker = fraud_rule_engine_service.Kafka.KafkaConsumerWorker;

namespace fraud_rule_engine_service.IntegrationTests.Kafka;

/// <summary>
/// End-to-end test of the messaging layer against a real, disposable Kafka broker (see
/// <see cref="KafkaFixture"/>) and a real Postgres database (see
/// <see cref="Persistence.PostgresFixture"/>) — the same production DI wiring
/// (<c>AddPersistence</c>/<c>AddFraudRuleEngine</c>/<c>AddKafkaMessaging</c>) used by
/// <c>Program.cs</c>, just pointed at test containers instead of the real infrastructure.
///
/// This complements, rather than replaces, the mocked-consumer unit tests in
/// <c>unit-test/Kafka/</c>: those prove the dispatch/commit/DLQ logic in isolation and run in
/// milliseconds; this proves the whole pipeline actually works against the real Kafka wire
/// protocol, including topic subscription, header propagation, and outbound publishing.
/// </summary>
[Collection(nameof(KafkaMessagingCollection))]
public sealed class KafkaMessagingFlowTests(PostgresFixture postgresFixture, KafkaFixture kafkaFixture) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private string _incomingTopic = null!;
    private string _raisedTopic = null!;

    public Task InitializeAsync()
    {
        // Unique topic/group names per test run so tests in this class never see each other's
        // messages, without needing to tear down and recreate the shared broker.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        _incomingTopic = $"test-transaction-categorized-events-{suffix}";
        _raisedTopic = $"test-fraud-case-raised-events-{suffix}";
        var dlqTopic = $"{_incomingTopic}.dlq";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ReadWrite"] = postgresFixture.ConnectionString,
                ["ConnectionStrings:ReadOnly"] = postgresFixture.ConnectionString,
                ["Kafka:BootstrapServers"] = kafkaFixture.BootstrapAddress,
                ["Kafka:ConsumerGroupId"] = $"test-group-{suffix}",
                ["Kafka:IncomingTopic"] = _incomingTopic,
                ["Kafka:DlqTopic"] = dlqTopic,
                ["Kafka:FraudCaseRaisedTopic"] = _raisedTopic
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistence(configuration);
        services.AddFraudRuleEngine(configuration);
        services.AddApplicationValidation();
        services.AddKafkaMessaging(configuration);

        _provider = services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task TransactionCategorizedEvent_PublishedToARealBroker_IsConsumedEvaluatedPersistedAndRepublished()
    {
        // Arrange: publish straight onto the real topic, exactly as the upstream categorization
        // system would — no test doubles anywhere on the messaging side.
        var transactionId = $"txn-{Guid.NewGuid()}";
        var @event = new TransactionCategorizedEvent
        {
            TransactionId = transactionId,
            AccountId = "acc-broker-test",
            CustomerId = "cust-broker-test",
            Amount = 95_000m, // above both HighValueThreshold (50,000) and NewPayeeAmountThreshold (15,000)
            Currency = "ZAR",
            Category = "transfer",
            Channel = TransactionChannel.Eft,
            IsNewPayee = true,
            CounterpartyAccountId = "payee-broker-test",
            CountryCode = "ZA",
            OccurredAt = DateTimeOffset.UtcNow
        };

        await PublishAsync(_incomingTopic, transactionId, TransactionCategorizedEvent.EventType, @event);

        // Act: start the real KafkaConsumerWorker — a real Confluent.Kafka consumer talking to the
        // real broker, wired through exactly the same DI registrations Program.cs uses.
        var worker = _provider.GetServices<IHostedService>().OfType<KafkaConsumerWorker>().Single();
        await worker.StartAsync(CancellationToken.None);

        try
        {
            // Assert: the fraud case shows up in the real Postgres read model...
            var fraudCase = await PollUntilAsync(
                async () =>
                {
                    using var scope = _provider.CreateScope();
                    var queryRepository = scope.ServiceProvider.GetRequiredService<IFraudCaseQueryRepository>();
                    var matches = await queryRepository.GetByTransactionIdAsync(transactionId, CancellationToken.None);
                    return matches.Count > 0 ? matches[0] : null;
                },
                TimeSpan.FromSeconds(20));

            fraudCase.ShouldNotBeNull();
            fraudCase.Severity.ShouldBe(FraudSeverity.High);
            fraudCase.TriggeredRules.ShouldContain(r => r.RuleCode == "HIGH_VALUE");
            fraudCase.TriggeredRules.ShouldContain(r => r.RuleCode == "NEW_PAYEE_LARGE_TRANSFER");

            // ...and the outbound fraud-case-raised event actually reached the real broker too.
            var raisedEvent = ConsumeOne<FraudCaseRaisedEvent>(_raisedTopic, TimeSpan.FromSeconds(15));
            raisedEvent.ShouldNotBeNull();
            raisedEvent.TransactionId.ShouldBe(transactionId);
            raisedEvent.FraudCaseId.ShouldBe(fraudCase.Id);
            raisedEvent.TriggeredRuleCodes.ShouldContain("HIGH_VALUE");
            raisedEvent.TriggeredRuleCodes.ShouldContain("NEW_PAYEE_LARGE_TRANSFER");
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private async Task PublishAsync<TEvent>(string topic, string key, string eventType, TEvent @event)
        where TEvent : DomainEvent
    {
        var config = new ProducerConfig { BootstrapServers = kafkaFixture.BootstrapAddress };
        using var producer = new ProducerBuilder<string, byte[]>(config).Build();

        var payload = JsonSerializer.SerializeToUtf8Bytes(@event, JsonSerialiserOptions.Default);
        var message = new Message<string, byte[]>
        {
            Key = key,
            Value = payload,
            Headers = new Headers { { DomainEventHeaders.EventType, Encoding.UTF8.GetBytes(eventType) } }
        };

        await producer.ProduceAsync(topic, message);
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    /// <summary>Reads a single message off <paramref name="topic"/>, from the earliest offset, within <paramref name="timeout"/>.</summary>
    private TEvent ConsumeOne<TEvent>(string topic, TimeSpan timeout)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaFixture.BootstrapAddress,
            GroupId = $"test-reader-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe(topic);

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(1));
            if (result is not null && !result.IsPartitionEOF)
            {
                return JsonSerializer.Deserialize<TEvent>(result.Message.Value, JsonSerialiserOptions.Default)
                    ?? throw new InvalidOperationException($"Payload on '{topic}' deserialized to null.");
            }
        }

        throw new TimeoutException($"No message appeared on '{topic}' within {timeout}.");
    }

    /// <summary>Polls <paramref name="probe"/> until it returns a non-null result or <paramref name="timeout"/> elapses.</summary>
    private static async Task<T?> PollUntilAsync<T>(Func<Task<T?>> probe, TimeSpan timeout) where T : class
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var result = await probe();
            if (result is not null)
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return null;
    }
}
