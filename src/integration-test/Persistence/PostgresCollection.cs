namespace fraud_rule_engine_service.IntegrationTests.Persistence;

[CollectionDefinition(nameof(PostgresCollection))]
public class PostgresCollection : ICollectionFixture<PostgresFixture>;
