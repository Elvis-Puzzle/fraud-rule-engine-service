-- The retrieval API reads through a role that can only ever SELECT, so a bug or compromise in
-- the query path can't mutate fraud-case data. A managed deployment would use IAM-authenticated
-- database access here rather than a static password.
CREATE ROLE fraud_rule_engine_reader WITH LOGIN PASSWORD 'fraud_rule_engine_reader_dev';
CREATE SCHEMA IF NOT EXISTS fraud_rule_engine AUTHORIZATION fraud_rule_engine;
GRANT USAGE ON SCHEMA fraud_rule_engine TO fraud_rule_engine_reader;
ALTER DEFAULT PRIVILEGES IN SCHEMA fraud_rule_engine GRANT SELECT ON TABLES TO fraud_rule_engine_reader;
