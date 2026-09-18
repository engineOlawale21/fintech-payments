# Fintech Payments API

A wallet and payment-processing API built with C#, ASP.NET Core 10, PostgreSQL, and Redis. It demonstrates atomic transfers, a double-entry ledger, retry-safe requests, signed provider webhooks, reconciliation, and settlement.

This is a portfolio project using a mock payment provider. It does not connect to a real payment network or include a wallet-funding endpoint.

## Features

- Registration and login with JWT authentication and role-based authorization.
- Customer-owned wallets and paginated ledger history.
- Atomic wallet transfers with balance checks, deterministic database locking, and idempotency keys.
- Balanced debit/credit ledger entries committed with each transfer.
- Signed mock-provider webhooks with replay protection and duplicate detection.
- Reconciliation, settlement batches, and append-only audit records for Operations/Admin users.
- Transactional outbox records for completed transfers, ready for asynchronous dispatch.
- Redis wallet-read caching, rate limits, correlation IDs, JSON logs, and health checks.
- EF Core migrations, automated tests, Docker Compose, and GitHub Actions.

## Requirements

- .NET 10 SDK (also needed to apply database migrations).
- Docker with Docker Compose.
- PowerShell for the commands and smoke-demo script below.

Run commands from the repository root.

## Quick start

### 1. Configure local secrets

```powershell
Copy-Item .env.example .env
```

Edit `.env` and replace all three placeholders. Use long random values for `JWT_SIGNING_KEY` and `WEBHOOK_SECRET` (at least 32 characters). Keep `.env` private; Git ignores it.

Docker Compose reads `.env` automatically. The .NET CLI does not, so set the database connection explicitly for migrations. Replace `YOUR_POSTGRES_PASSWORD` with the same password used in `.env`.

### 2. Start PostgreSQL and Redis, then apply migrations

```powershell
docker compose up -d --wait postgres redis
$env:Database__ConnectionString = "Host=localhost;Port=5432;Database=fintech_payments;Username=fintech;Password=YOUR_POSTGRES_PASSWORD"
dotnet restore FintechPayments.sln --locked-mode
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/FintechPayments.Infrastructure --startup-project src/FintechPayments.Api
```

Migrations are applied explicitly, not automatically during API startup.

### 3. Start the API

```powershell
docker compose up -d --build api
Invoke-RestMethod http://localhost:8080/health/live
Invoke-RestMethod http://localhost:8080/health/ready
```

| Resource | Docker address |
| --- | --- |
| API | `http://localhost:8080` |
| OpenAPI JSON (Development) | `http://localhost:8080/openapi/v1.json` |
| Process health | `http://localhost:8080/health/live` |
| PostgreSQL and Redis readiness | `http://localhost:8080/health/ready` |

The project publishes an OpenAPI document; it does not configure a Swagger UI.

### Run the API with the .NET SDK instead

With PostgreSQL/Redis running and migrations applied, keep the database environment variable from step 2 and set the remaining overrides:

```powershell
$env:Redis__ConnectionString = "localhost:6379,abortConnect=false"
$env:Jwt__SigningKey = "YOUR_JWT_SIGNING_KEY_FROM_ENV"
$env:Webhooks__MockProvider__Secret = "YOUR_WEBHOOK_SECRET_FROM_ENV"
dotnet run --project src/FintechPayments.Api --launch-profile http
```

The launch profile uses `http://localhost:5066` and the Development environment. Use that port for health checks and OpenAPI when running through the SDK. Checked-in Development settings contain local demonstration values only.

### Stop or inspect the stack

```powershell
docker compose logs --tail 100 api
docker compose down
```

Database and Redis data remain in named Docker volumes after shutdown. Changing `POSTGRES_PASSWORD` in `.env` does not change the password in an already initialized database volume.

## Try the API

For a scripted demo against the Docker API:

```powershell
.\scripts\smoke-demo.ps1 -WebhookSecret "YOUR_WEBHOOK_SECRET_FROM_ENV"
```

For an SDK-hosted API, also pass `-BaseUrl "http://localhost:5066"`.

The script creates disposable users and wallets, promotes a demo user to Operations through the local Docker database, checks insufficient-funds protection and duplicate webhook handling, runs reconciliation, checks that unmatched inputs cannot settle, and reads audit events. It requires the local Compose database and is intended for development.

See [API examples](docs/api-examples.md) for request bodies and authentication examples.

| Area | Example endpoints |
| --- | --- |
| Authentication | `POST /api/v1/auth/register`, `POST /api/v1/auth/login` |
| Wallets | `POST /api/v1/wallets`, `GET /api/v1/wallets/{id}` |
| Transfers | `POST /api/v1/transfers`, `GET /api/v1/transfers/{id}` |
| Ledger | `GET /api/v1/wallets/{id}/ledger` |
| Provider events | `POST /api/v1/webhooks/payments/mock-provider` |
| Reconciliation | `POST /api/v1/reconciliations` |
| Settlements | `POST /api/v1/settlements` |
| Audit | `GET /api/v1/audit-events` |

Transfers require an `Idempotency-Key` header. Reusing a key for the same request returns the recorded result; conflicting reuse is rejected. Reconciliation, settlement, and audit routes require Operations or Admin access.

## Project structure

```text
src/
  FintechPayments.Api/             Controllers, contracts, authentication, middleware
  FintechPayments.Application/     Use cases, validation, persistence interfaces
  FintechPayments.Domain/          Wallet, transfer, ledger, and settlement rules
  FintechPayments.Infrastructure/  EF Core, migrations, PostgreSQL, Redis, JWT
 tests/                           Unit and API integration tests
 docs/                            Design, security, and API documentation
 scripts/                         Local smoke demo
```

PostgreSQL is the source of truth. Transfer balances, ledger entries, and transfer state commit in one database transaction. Redis caches wallet reads and is not authoritative for financial state. Monetary values use C# `decimal` and fixed-precision PostgreSQL columns.

## Build and test

```powershell
dotnet restore FintechPayments.sln --locked-mode
dotnet build FintechPayments.sln --configuration Release --no-restore
dotnet test FintechPayments.sln --configuration Release --no-build --no-restore
```

Unit tests cover domain and application behavior. Integration tests cover HTTP routing, validation, authorization, error handling, OpenAPI, and persistence mappings; they skip live dependency connectivity checks. Use the Docker smoke demo for live infrastructure verification.

GitHub Actions runs a locked restore, Release build, tests with coverage, and a Docker image build on pushes and pull requests to `main`.

## What belongs in Git

Commit source code, migrations, the checked-in `artifacts/migrations.sql` script, `packages.lock.json`, the tool manifest, shared configuration templates, Docker/CI definitions, and documentation.

The [`.gitignore`](.gitignore) excludes:

- Build/package output: `bin/`, `obj/`, generated artifact output, `publish/`, NuGet packages. The checked-in `artifacts/migrations.sql` script is allowed.
- Test results, coverage reports, logs, and crash dumps.
- IDE state and operating-system files.
- `.env` variants (except `.env.example`), local settings, secrets, certificates, and private keys.
- Local Docker overrides, database dumps/data, and temporary files.
- Private agent settings and working notes.

Ignoring a local settings file does not make the application load it automatically. Use environment variables for local overrides. Never put real credentials into tracked `appsettings` files. Ignore rules also do not remove files already tracked by Git.

## Documentation

- [Product requirements](docs/01-product-requirements.md)
- [Implementation plan](docs/02-implementation-plan.md) and [project progress](PROJECT_PLAN.md)
- [Database design](docs/database.md)
- [Architecture decisions](docs/architecture-decisions.md)
- [Threat model](docs/threat-model.md) and [security policy](SECURITY.md)
- [API examples](docs/api-examples.md)
- [Portfolio and interview notes](docs/cv-and-interview.md)

## Limitations and next steps

This project is not a production payment service. Production extensions include an outbox dispatcher with retries and dead-letter handling, real provider integrations, managed secrets and key rotation, durable data-protection keys, operational role provisioning, telemetry export, and load testing. Transfer completion events are persisted atomically; audit delivery still happens after successful operations until it is routed through the outbox.
