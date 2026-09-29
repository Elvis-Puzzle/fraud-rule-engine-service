using fraud_rule_engine_service.IntegrationTests.Persistence;

namespace fraud_rule_engine_service.IntegrationTests.Kafka;

/// <summary>
/// Shares one real Postgres container and one real Kafka broker across every test in this
/// collection, rather than starting a fresh container per test (slow) or per class.
/// </summary>
[CollectionDefinition(nameof(KafkaMessagingCollection))]
public class KafkaMessagingCollection : ICollectionFixture<PostgresFixture>, ICollectionFixture<KafkaFixture>;
