# Fraud Rule Engine Service

Consumes categorized transaction events from Kafka, evaluates each transaction against a
configurable set of fraud rules, persists any resulting fraud case, and exposes a retrieval API
for querying, filtering, and reporting on those cases.

Built for the internal promotion Backend project brief ("Fraud Rule Engine Service"): *"Create a
system that processes categorized transaction events and flags potential fraud. Apply a set of
fraud rules per transaction based on different criteria and then store them in a data store. Allow
the retrieval of this data via an API."*

## Architecture

```
Kafka (transaction-categorized-events)
        │
        ▼
KafkaConsumerWorker ──▶ ServiceMessageHandler ──▶ EventHandlerRegistry ──▶ TransactionCategorizedEventHandler
                                                                                   │
                                                                                   ▼
                                                                      FraudEvaluationService
                                                                     (runs every IFraudRule)
                                                                                   │
                                                    ┌──────────────────────────────┼───────────────────────────┐
                                                    ▼                              ▼                            ▼
                                     ApplicationReadWriteContext         fraud-case-raised-events        (structured logs)
                                        (Postgres, write role)                (Kafka, outbound)

Controllers (FraudCasesController / RulesController)
        │
        ▼
FraudCaseQueryService ──▶ ApplicationReadOnlyContext (Postgres, read-only role)
```

- **Rule engine** (`Domain/Rules`): eight independent, unit-testable rules (`IFraudRule`), each
  looking only at the current transaction plus a bounded window of the account's recent history —
  no rule talks to Kafka or the database directly.
  - `HIGH_VALUE`, `VELOCITY`, `STRUCTURING`, `IMPOSSIBLE_TRAVEL`, `UNUSUAL_HOUR`,
    `NEW_PAYEE_LARGE_TRANSFER`, `BLACKLISTED_MERCHANT`, `DUPLICATE`
  - Thresholds are all configurable via the `FraudRules` section in `appsettings.json` — no
    redeploy needed to tune risk appetite.
  - `GET /api/v1/rules` lists every registered rule for transparency/auditing.
- **Messaging** (`Kafka/`): consumes `transaction-categorized-events`, dispatches by the
  `event-type` header via an `EventHandlerRegistry` (so adding a new inbound event type is just
  registering another `IServiceEventHandler`, no dispatcher changes). On a handler failure, the
  message — with its original headers plus the error — is routed to a DLQ topic rather than
  blocking the partition as a poison pill; if the DLQ publish itself fails, the offset is left
  uncommitted (retried on restart) rather than crashing the host. Kafka is at-least-once, so
  redelivery of an already-processed transaction is detected and answered from the existing
  result rather than being re-evaluated (`FraudEvaluationService.EvaluateAsync`). Raised fraud
  cases are published to `fraud-case-raised-events` for downstream consumers (case management,
  notifications, etc.).
- **Persistence** (`Persistence/`): EF Core over Postgres, with a read/write split — the write
  path uses a normal role, the retrieval API queries through a separate, least-privilege
  **read-only** Postgres role that can only ever `SELECT` (see `scripts/init-db`).
- **Retrieval API** (`Controllers/`): `GET /api/v1/fraud-cases` (filter by account, customer,
  severity, status, rule, date range; paginated), `GET /api/v1/fraud-cases/{id}`,
  `GET /api/v1/fraud-cases/by-transaction/{transactionId}`, `GET /api/v1/fraud-cases/summary`
  (aggregate counts by severity/rule for dashboards), `PATCH /api/v1/fraud-cases/{id}/status`
  (investigation workflow — requires the `FraudAnalyst` role, not just any authenticated caller).
  Every endpoint requires a JWT bearer token.

## Prerequisites

- .NET 10 SDK
- Docker (with Compose v2)

## Running it

### 1. Mint a local dev JWT (once)

The API requires a bearer token. `dotnet user-jwts` (built into the .NET SDK) generates one signed
with a throwaway, machine-local dev key — no real identity provider needed for local/demo use.

```bash
cd src/fraud-rule-engine-service
dotnet user-jwts create --name docker-demo --audience http://localhost:8080 --role FraudAnalyst
```

Copy the printed `Token:` value — you'll pass it as `Authorization: Bearer <token>` on every
request below.

### 2. Run everything in Docker

```bash
docker compose build
docker compose up -d
```

`docker compose build` is a genuine multi-stage build — it restores, builds, and publishes the
app *inside* the Docker build itself, so this works straight from a fresh clone with no host-side
.NET SDK step required (it does need normal outbound internet access to reach nuget.org during
the build, same as any .NET Dockerfile).

The build trusts Zscaler's public root CA (`certs/zscaler-root-ca.pem`) before restoring, so this
works unmodified on a network that routes outbound HTTPS through it. On a different
TLS-inspecting proxy, see `certs/README.md`.

This starts Postgres, a single-node Kafka broker (KRaft mode), runs EF Core migrations via a
one-shot `migrate` service, and then the API on **http://localhost:8080**.

Check it's up:

```bash
curl http://localhost:8080/health/liveness
curl http://localhost:8080/health/readiness
curl http://localhost:8080/api/v1/rules -H "Authorization: Bearer <token>"
```

### 3. Feed it some transactions

```bash
./scripts/publish-sample-events.sh
```

Publishes five sample `transaction-categorized-event` messages (one clean, four fraud-triggering)
straight to Kafka. Watch them get evaluated:

```bash
docker compose logs -f fraud-rule-engine-service
```

Then query the results:

```bash
curl "http://localhost:8080/api/v1/fraud-cases?minSeverity=high" -H "Authorization: Bearer <token>"
curl "http://localhost:8080/api/v1/fraud-cases/summary" -H "Authorization: Bearer <token>"
curl "http://localhost:8080/api/v1/fraud-cases/by-transaction/txn-blacklist-1" -H "Authorization: Bearer <token>"
```

Transitioning a case's status requires the `FraudAnalyst` role (already on the token minted
above) — any authenticated caller without that role gets a `403`:

```bash
curl -X PATCH "http://localhost:8080/api/v1/fraud-cases/<id>/status" \
  -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"status":"confirmed"}'
```

Swagger UI is at **http://localhost:8080/swagger** (non-Production environments only).

### 4. Tear down

```bash
docker compose down -v
```

### Running without Docker

```bash
cd src/fraud-rule-engine-service
dotnet run -- -m     # apply migrations against a local Postgres (see appsettings.Development.json)
dotnet run            # start the API
```

## Testing

```bash
cd src
dotnet test unit-test/fraud-rule-engine-service.UnitTests.csproj         # 62 tests, no external deps
dotnet test integration-test/fraud-rule-engine-service.IntegrationTests.csproj  # 5 tests, spins up a real Postgres via Testcontainers — needs Docker
```

Unit tests cover every rule's trigger/no-trigger boundaries (including the `UnusualHourRule`
overnight wrap-around branch), the evaluation/aggregation service (including the Kafka-redelivery
idempotency path), the options cross-validation, and the controllers — plus the messaging layer
itself: `EventHandlerRegistry`'s dispatch/duplicate-registration behaviour, `ServiceMessageHandler`,
`TransactionCategorizedEventHandler`, and `KafkaConsumerWorker`'s commit/DLQ/crash-guard logic
(the consumer is built via DI as `IConsumer<string, byte[]>` specifically so a mocked consumer can
be injected in tests, rather than requiring a real broker). Integration tests exercise the
repositories against a real Postgres container rather than an in-memory provider, so they catch
provider-specific issues (naming convention, precision, migrations) that an in-memory double
would hide.

## Configuration

All fraud-rule thresholds live under `FraudRules` in `appsettings.json` (sub-threshold amounts,
velocity windows, blacklists, etc.) — see `Domain/Rules/FraudRuleOptions.cs` for the full list and
defaults. Kafka topics/connection and the Postgres connection strings are under `Kafka` and
`ConnectionStrings` respectively.

## What's intentionally out of scope for this demo

- **A managed identity provider** (e.g. Keycloak, Auth0, Cognito) — replaced with `dotnet user-jwts`
  for local/demo use; the app only depends on standard JWT bearer validation, so swapping the
  issuer is a configuration change, not a code change.
- **Secrets manager / IAM database auth** (e.g. AWS Secrets Manager, RDS IAM tokens) — replaced
  with plain connection strings for local/demo use.
- **A Kafka-broker-backed integration test** — the messaging layer has full unit coverage with a
  mocked consumer (see Testing below), but no test yet spins up a real broker via Testcontainers
  the way the Postgres tests do.
