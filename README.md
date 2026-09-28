# Fraud Rule Engine Service

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Kafka](https://img.shields.io/badge/Kafka-3.9-231F20?logo=apachekafka&logoColor=white)
![Postgres](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![Tests](https://img.shields.io/badge/tests-67%20passing-brightgreen)

Consumes categorized transaction events from Kafka, evaluates each transaction against a
configurable set of fraud rules, persists any resulting fraud case, and exposes a retrieval API
for querying, filtering, and reporting on those cases.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick Start (Docker)](#quick-start-docker)
- [Running Without Docker](#running-without-docker)
- [API Reference](#api-reference)
- [Testing](#testing)
- [Configuration](#configuration)
- [Roadmap / Out of Scope](#roadmap--out-of-scope)

## Overview

| | |
|---|---|
| **Input** | `transaction-categorized-events` (Kafka) |
| **Output** | `fraud-case-raised-events` (Kafka) + queryable REST API |
| **Rules engine** | 8 independent, configurable fraud rules (see [Configuration](#configuration)) |
| **Storage** | PostgreSQL, with a read/write role split for least-privilege querying |
| **Auth** | JWT bearer tokens, with role-based access on write endpoints |

## Architecture

The service has two independent paths that only meet at the database: an **ingestion path**
(Kafka → rule evaluation → persistence) and a **retrieval path** (REST API → read-only queries).

### Ingestion & evaluation path

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
```

### Retrieval path

```
Controllers (FraudCasesController / RulesController)
        │
        ▼
FraudCaseQueryService ──▶ ApplicationReadOnlyContext (Postgres, read-only role)
```

### Components at a glance

| Component | Responsibility | Location |
|---|---|---|
| `KafkaConsumerWorker` | Long-running background consumer; owns commit/DLQ/crash-guard logic | `Kafka/KafkaConsumerWorker.cs` |
| `ServiceMessageHandler` | Deserialises the raw Kafka message, reads the `event-type` header | `Kafka/ServiceMessageHandler.cs` |
| `EventHandlerRegistry` | Dispatches to the matching `IServiceEventHandler` by event type | `Kafka/EventHandlerRegistry.cs` |
| `TransactionCategorizedEventHandler` | Validates the event, hands it to the evaluation service | `Kafka/Handlers/TransactionCategorizedEventHandler.cs` |
| `FraudEvaluationService` | Runs every `IFraudRule`, aggregates results, persists, publishes | `Service/FraudEvaluationService.cs` |
| `IFraudRule` implementations | One class per fraud check, independently unit-tested | `Domain/Rules/` |
| `ApplicationReadWriteContext` | EF Core context used for the write path (evaluation results) | `Persistence/ApplicationReadWriteContext.cs` |
| `ApplicationReadOnlyContext` | EF Core context used for the retrieval API, backed by a `SELECT`-only DB role | `Persistence/ApplicationReadOnlyContext.cs` |
| `FraudCasesController` / `RulesController` | REST API surface | `Controllers/` |
| `FraudCaseQueryService` | Filtering/pagination/aggregation logic behind the retrieval API | `Service/FraudCaseQueryService.cs` |

### Key design decisions

#### Rule engine (`Domain/Rules`)

Eight independent, unit-testable rules (`IFraudRule`), each looking only at the current
transaction plus a bounded window of the account's recent history — no rule talks to Kafka or the
database directly.

`HIGH_VALUE` · `VELOCITY` · `STRUCTURING` · `IMPOSSIBLE_TRAVEL` · `UNUSUAL_HOUR` ·
`NEW_PAYEE_LARGE_TRANSFER` · `BLACKLISTED_MERCHANT` · `DUPLICATE`

- Thresholds are all configurable via the `FraudRules` section in `appsettings.json` — no
  redeploy needed to tune risk appetite.
- `GET /api/v1/rules` lists every registered rule for transparency/auditing.

#### Messaging (`Kafka/`)

- Consumes `transaction-categorized-events`, dispatches by the `event-type` header via an
  `EventHandlerRegistry` (so adding a new inbound event type is just registering another
  `IServiceEventHandler`, no dispatcher changes).
- On a handler failure, the message — with its original headers plus the error — is routed to a
  DLQ topic rather than blocking the partition as a poison pill; if the DLQ publish itself fails,
  the offset is left uncommitted (retried on restart) rather than crashing the host.
- Kafka is at-least-once, so redelivery of an already-processed transaction is detected and
  answered from the existing result rather than being re-evaluated
  (`FraudEvaluationService.EvaluateAsync`).
- Raised fraud cases are published to `fraud-case-raised-events` for downstream consumers (case
  management, notifications, etc.).

#### Persistence (`Persistence/`)

EF Core over Postgres, with a read/write split — the write path uses a normal role, the retrieval
API queries through a separate, least-privilege **read-only** Postgres role that can only ever
`SELECT` (see `scripts/init-db`).

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 / ASP.NET Core |
| Messaging | Apache Kafka 3.9 (KRaft mode) |
| Database | PostgreSQL 16, EF Core |
| Auth | JWT bearer (`dotnet user-jwts` for local dev) |
| Containerization | Docker, Docker Compose v2 |
| Testing | xUnit, Shouldly, Testcontainers |

## Prerequisites

- .NET 10 SDK
- Docker (with Compose v2)

## Quick Start (Docker)

### 1. Mint a local dev JWT (once)

The API requires a bearer token. `dotnet user-jwts` (built into the .NET SDK) generates one signed
with a throwaway, machine-local dev key — no real identity provider needed for local development.

```bash
cd src/fraud-rule-engine-service
dotnet user-jwts create --name local-dev --audience http://localhost:8080 --role FraudAnalyst
cd ../..
```

Copy the printed `Token:` value — you'll pass it as `Authorization: Bearer <token>` on every
request below. The `cd ../..` returns you to the repo root, where every command below assumes
you are there.

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

## Running Without Docker

This runs the API process directly via the .NET SDK — Postgres and Kafka still need to be
running somewhere, most simply by starting just those two from the same compose file:

```bash
docker compose up -d postgres kafka
cd src/fraud-rule-engine-service
dotnet run -- -m     # apply migrations against the connection strings in appsettings.Development.json
dotnet run            # start the API — listens on http://localhost:5138, not :8080 (see launchSettings.json)
```

Note the different port: minting a token still works the same way (`--audience` only needs to
match `ValidAudiences` in configuration, not the actual port), but point `curl`/Swagger at `:5138`
instead of `:8080` when running this way. `dotnet run` runs in the foreground — `Ctrl+C` to stop
it, then `cd ../..` to return to the repo root before continuing.

## API Reference

All endpoints require a JWT bearer token (`Authorization: Bearer <token>`).

| Method | Endpoint | Description | Role required |
|---|---|---|---|
| `GET` | `/api/v1/rules` | List every registered fraud rule | any authenticated caller |
| `GET` | `/api/v1/fraud-cases` | List/filter fraud cases (account, customer, severity, status, rule, date range; paginated) | any authenticated caller |
| `GET` | `/api/v1/fraud-cases/{id}` | Get a single fraud case by ID | any authenticated caller |
| `GET` | `/api/v1/fraud-cases/by-transaction/{transactionId}` | Get the fraud case (if any) for a transaction | any authenticated caller |
| `GET` | `/api/v1/fraud-cases/summary` | Aggregate counts by severity/rule, for dashboards | any authenticated caller |
| `PATCH` | `/api/v1/fraud-cases/{id}/status` | Transition a case's investigation status | `FraudAnalyst` |
| `GET` | `/health/liveness` | Liveness probe | none |
| `GET` | `/health/readiness` | Readiness probe (checks DB/Kafka) | none |

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

## Roadmap 

The following were deliberately left out to keep the project focused — each is a conscious
trade-off, not an oversight, and the reasoning behind leaving it out is included below.

- **A managed identity provider** (e.g. Keycloak, Auth0, Cognito) instead of `dotnet user-jwts` —
  the app only depends on standard JWT bearer validation, so swapping the issuer is a
  configuration change, not a code change.
- **Secrets manager / IAM database auth** (e.g. AWS Secrets Manager, RDS IAM tokens) instead of
  plain connection strings.
- **A Kafka-broker-backed integration test** — the messaging layer has full unit coverage with a
  mocked consumer (see Testing above), but no test yet spins up a real broker via Testcontainers
  the way the Postgres tests do.
