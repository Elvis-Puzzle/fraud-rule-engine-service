using Testcontainers.Kafka;

namespace fraud_rule_engine_service.IntegrationTests.Kafka;

/// <summary>
/// Spins up a real, single-node Kafka broker (KRaft mode, no ZooKeeper) via Testcontainers — the
/// same <c>apache/kafka</c> image family used in <c>docker-compose.yml</c> — so the messaging
/// tests exercise the real Confluent.Kafka wire protocol rather than a mocked
/// <c>IConsumer&lt;,&gt;</c>, the way <see cref="Persistence.PostgresFixture"/> does for Postgres.
/// </summary>
public sealed class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder("apache/kafka:3.9.0")
        .WithKRaft()
        .Build();

    public string BootstrapAddress => _container.GetBootstrapAddress();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
