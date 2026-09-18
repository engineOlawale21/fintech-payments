# Implementation plan

## Technical baseline

- Target framework: `.NET 10` (`net10.0`).
- API style: ASP.NET Core controllers.
- Architecture: pragmatic Clean Architecture with Domain, Application, Infrastructure, and API projects.
- Persistence: EF Core with PostgreSQL.
- Cache and coordination: Redis.
- Tests: xUnit unit tests plus HTTP integration tests using real PostgreSQL and Redis containers.
- Local runtime: Docker Compose.
- CI: GitHub Actions on Linux.

Package versions should be centrally managed and pinned when implementation begins. Framework-compatible stable versions should be selected at that time instead of embedding speculative version numbers in this plan.

## Delivery strategy

Implement vertical slices in dependency order. Each slice must leave the repository buildable, tested, and demonstrable. Financial correctness work takes priority over adding more endpoints.

```text
Foundation -> Persistence -> Authentication -> Wallets -> Ledger
           -> Transfers -> Idempotency -> Webhooks
           -> Reconciliation -> Settlement -> Production polish
```

## Project structure

```text
FintechPayments.sln
Directory.Build.props
Directory.Packages.props
src/
  FintechPayments.Domain/
  FintechPayments.Application/
  FintechPayments.Infrastructure/
  FintechPayments.Api/
tests/
  FintechPayments.UnitTests/
  FintechPayments.IntegrationTests/
docs/
Dockerfile
compose.yaml
.dockerignore
.github/workflows/ci.yml
```

Dependency direction:

```text
Api -> Application <- Infrastructure
            |
          Domain
```

Rules:

- Domain references no other project.
- Application references Domain only.
- Infrastructure references Application and Domain.
- API references Application and Infrastructure for composition.
- Controllers do not access `DbContext` directly.
- Domain entities contain no EF Core, ASP.NET Core, or JSON concerns.
- PostgreSQL, not Redis, is authoritative for financial correctness.

## Work package 0 — solution foundation

### Tasks

1. Create the solution and six projects.
2. Add project references following the dependency rules.
3. Enable nullable references, implicit usings, analyzers, deterministic builds, and warnings-as-errors in CI.
4. Add central package management and `.editorconfig`.
5. Configure controllers, consistent JSON, OpenAPI, and problem details.
6. Add correlation-ID and global exception-handling middleware.
7. Configure structured console logging and sensitive-field redaction.
8. Add `/health/live` and `/health/ready`.
9. Add unit and integration smoke tests.

Package categories: EF Core/Npgsql, JWT bearer authentication, OpenAPI, Redis, xUnit, an assertion library, ASP.NET Core integration testing, container-based integration testing, structured logging, and architecture testing.

### Gate

- Restore, build, and tests succeed.
- API starts and publishes OpenAPI.
- Liveness works without dependencies.
- Readiness accurately reports dependency status.

Suggested commit: `chore: scaffold clean architecture solution and API foundation`

## Work package 1 — Docker and CI

### Tasks

1. Add PostgreSQL and Redis to `compose.yaml` with health checks and named volumes.
2. Add a multi-stage API Dockerfile running as a non-root user.
3. Add environment configuration with startup validation.
4. Add `.env.example` with names and safe samples only.
5. Add GitHub Actions jobs for restore, build, unit tests, integration tests, and Docker build.
6. Cache NuGet packages and publish test/coverage artifacts.

### Gate

- `docker compose up --build` starts the stack from a clean clone.
- PostgreSQL and Redis readiness checks pass.
- CI runs against real disposable dependencies.
- No secret is committed or echoed.

Suggested commit: `build: add Docker development stack and continuous integration`

## Work package 2 — persistence foundation

### Tasks

- Add encapsulated identifiers, UTC `IClock`, domain exceptions, `Currency`, and money validation.
- Create `PaymentsDbContext` and explicit entity configurations.
- Configure PostgreSQL `numeric(19,4)`, UTC timestamps, enum conversions, indexes, uniqueness, checks, and concurrency fields.
- Add design-time context creation and the reviewed initial migration.
- Add Development-only database initialization.

### Gate

- Migration applies to an empty PostgreSQL database and rolls back in a disposable test database.
- Precision, uniqueness, check constraints, and concurrency fields are integration-tested.
- EF Core's in-memory provider is not used for relational behavior.

Suggested commit: `feat: establish PostgreSQL persistence and financial value types`

## Work package 3 — authentication and authorization

### Model and use cases

- `User`: ID, normalized email, password hash, role, status, timestamps.
- Roles: `Customer`, `Operations`, `Admin`; statuses: `Active`, `Disabled`.
- Implement `RegisterUser`, `LoginUser`, and `GetCurrentUser`.
- Define token and password service abstractions.

### Endpoints

- `POST /api/v1/auth/register`.
- `POST /api/v1/auth/login`.
- `GET /api/v1/users/me`.

### Security

- Use the platform password hasher.
- Validate JWT issuer, audience, signature, lifetime, and clock skew.
- Emit only required claims: subject, role, and token ID.
- Add ownership and operations authorization policies.
- Rate-limit registration and login.

### Gate

- Email normalization and uniqueness work.
- Hashes and secrets never appear in responses or logs.
- Invalid, expired, wrongly signed, and disabled-user tokens fail correctly.
- Tests distinguish `401 Unauthorized` from `403 Forbidden`.

Suggested commit: `feat: add JWT authentication and role-based authorization`

## Work package 4 — wallets

### Tasks

- Model wallet ID, owner, currency, available balance, `Active/Frozen/Closed` status, concurrency version, and timestamps.
- Implement `CreateWallet`, `GetWallet`, and `ListMyWallets`.
- Add `POST /api/v1/wallets`, `GET /api/v1/wallets`, and `GET /api/v1/wallets/{id}`.
- Enforce one wallet per owner/currency and resource ownership.
- Add deterministic Development/test funding through a balanced ledger posting, never a naked balance update.

### Gate

- Duplicate owner/currency returns conflict.
- Customers cannot read other customers' wallets.
- Operations access follows an explicit policy.
- Responses do not expose concurrency internals.

Suggested commit: `feat: implement owned multi-currency wallets`

## Work package 5 — double-entry ledger

### Tasks

- Model immutable `LedgerTransaction` and `LedgerEntry` records.
- Require every posting to balance total debits and credits by currency.
- Debit the source wallet and credit the destination wallet for a transfer.
- Use a balancing platform account for funding fixtures.
- Treat stored wallet balances as transactional projections rebuildable from the ledger.
- Add `GET /api/v1/wallets/{id}/ledger` with cursor pagination.

### Gate

- An unbalanced posting cannot be constructed or saved.
- Posted entries cannot be edited through application behavior.
- Pagination remains stable when newer entries arrive.
- Rebuilt balances equal stored wallet balances.

Suggested commit: `feat: add immutable double-entry transaction ledger`

## Work package 6 — atomic transfers and concurrency

### Model

- Transfer fields: source, destination, amount, currency, client reference, `Pending/Completed/Failed` status, failure reason, timestamps.
- State changes occur through explicit methods, not public setters.

### Transaction algorithm

1. Validate the command before opening a transaction where possible.
2. Begin a PostgreSQL transaction.
3. Load both wallets with row locks in deterministic ID order.
4. Recheck ownership, status, currency, amount, and funds inside the transaction.
5. Create the pending transfer.
6. Post balanced source-debit and destination-credit entries.
7. Update both projected balances.
8. Complete the transfer and commit once.

Use deterministic pessimistic row locking first because it is clear and PostgreSQL-specific. Retry only recognized transient or deadlock failures with a bounded policy; never retry business failures.

### Endpoints

- `POST /api/v1/transfers`.
- `GET /api/v1/transfers/{id}`.
- `GET /api/v1/transfers` with filters and cursor pagination.

### Gate

- Insufficient funds and injected faults create no partial posting.
- Concurrent transfers cannot make a balance negative.
- Opposing transfers use the same lock order.
- Ledger-derived and projected balances agree after concurrency tests.

Suggested commit: `feat: implement atomic and concurrency-safe wallet transfers`

## Work package 7 — persistent idempotency

### Model

```text
InProgress -> Completed
InProgress -> FailedRetryable
InProgress -> FailedFinal
```

Persist caller, endpoint, key, canonical SHA-256 request hash, state, response status/content type/body, timestamps, and expiry under a unique caller/endpoint/key constraint.

### Request behavior

1. Require `Idempotency-Key` for transfer creation.
2. Canonicalize command fields and hash them.
3. Insert a reservation under the database unique constraint.
4. Execute once and associate the durable result.
5. Replay a completed response when the key and hash match.
6. Return `409` if the key exists with a different hash.
7. Return a stable retry response while an operation is genuinely in progress.

An unknown result after a commit attempt must be resolved from PostgreSQL before retrying. Redis may cache completed results or coordinate briefly, but cache loss cannot permit duplicate money movement.

### Gate

- Sequential and concurrent identical retries create one transfer.
- Conflicting key reuse returns `409`.
- Restarting API and Redis does not permit duplication.
- Stored responses exclude secrets and mutable headers.

Suggested commit: `feat: protect transfer commands with durable idempotency`

## Work package 8 — signed webhooks

### Tasks

- Define a versioned mock-provider schema containing event ID/type, resource reference, money, provider timestamp, and data.
- Sign `timestamp + "." + exact UTF-8 body` using HMAC-SHA256.
- Add signature-version and timestamp headers, replay tolerance, request-size limits, and constant-time comparison.
- Persist the event before processing under unique `(Provider, ExternalEventId)`.
- Return success for an already accepted duplicate without repeating its effect.
- Record processed/failed state and auditable failure reason.

### Gate

- Valid signatures succeed.
- Changed bodies, stale timestamps, unsupported signature versions, and wrong secrets fail.
- Concurrent duplicate events cause one effect.
- Processing failures remain visible and safely retryable.

Suggested commit: `feat: verify and deduplicate payment provider webhooks`

## Work package 9 — reconciliation

### Tasks

- Document a JSON or CSV mock-provider statement format and checksum it.
- Reserve a unique provider/period/checksum reconciliation run.
- Normalize internal and external records and match on stable references.
- Categorize matched, missing-internal, missing-external, and amount-mismatch records.
- Persist totals and items transactionally.
- Allow Operations/Admin to add resolution notes without erasing history.

### Endpoints

- `POST /api/v1/reconciliations`.
- `GET /api/v1/reconciliations/{id}`.
- `GET /api/v1/reconciliations/{id}/items`.
- `POST /api/v1/reconciliations/{id}/items/{itemId}/resolve`.

### Gate

- A fixture produces deterministic categories and totals.
- Identical input is idempotent.
- Run totals equal persisted item sums.
- Only Operations/Admin can run and resolve reconciliation.

Suggested commit: `feat: add auditable provider reconciliation workflow`

## Work package 10 — settlement

### Tasks

- Define and document a mock fee formula.
- Include only completed, reconciled, unsettled transactions.
- Persist exact input identifiers for reproducibility.
- Implement `Draft -> Finalized`; finalized batches are immutable.
- Represent corrections as later adjustments.

### Endpoints

- `POST /api/v1/settlements`.
- `GET /api/v1/settlements/{id}`.
- `GET /api/v1/settlements`.
- `POST /api/v1/settlements/{id}/finalize`.

### Gate

- Gross, fee, adjustment, and net calculations are unit-tested.
- Ineligible/already-settled transfers are excluded.
- Finalized periods cannot overlap by provider and currency.
- Concurrent finalization cannot settle a transfer twice.

Suggested commit: `feat: generate reproducible settlement batches`

## Work package 11 — operational hardening

### Tasks

- Add append-only audit events for authentication, transfer, reconciliation, settlement, and administration.
- Add endpoint-specific rate limits, I/O timeouts, cancellation, and graceful shutdown.
- Add stable structured event IDs and redaction tests.
- Optimize reads with projections, cursor pagination, inspected SQL, and justified indexes.
- Add architecture dependency tests.

### Gate

- Expected business failures are not logged repeatedly as exceptions.
- Unexpected errors return safe problem details with correlation IDs.
- Health endpoints and logs diagnose dependency failures.
- Authentication, transfer, webhook, and idempotency routes are rate-limited.

Suggested commit: `feat: add auditability observability and API hardening`

## Work package 12 — recruiter-ready documentation

### Tasks

- Replace planned README language with verified behavior only.
- Add exact clean-clone setup, migrations, configuration, and demo steps.
- Add architecture, transfer sequence, and database diagrams.
- Add example HTTP requests and expected responses.
- Document security/scalability decisions and limitations.
- Add decision records for architecture, ledger, concurrency, and idempotency.
- Add CI and coverage badges only after they exist.
- Add the truthful CV project description.

### Gate

- Another person can perform the five-minute demo solely from the README.
- Every documented command works from a clean environment.
- Metrics and screenshots are accurately labeled.
- No placeholder secret, false scale figure, or production claim remains.

Suggested commit: `docs: complete recruiter-ready project walkthrough`

## Test implementation plan

### Unit tests

- Money and currency validation.
- Wallet state and balance rules.
- Transfer state transitions.
- Balanced ledger construction.
- Idempotency fingerprints and decisions.
- Webhook signatures and timestamp validation.
- Reconciliation classification.
- Settlement arithmetic.

### Integration-test harness

- Start isolated PostgreSQL and Redis containers.
- Replace production configuration with generated test settings.
- Apply real migrations.
- Reset database state safely between tests.
- Provide helpers for registration, login, wallet fixtures, and signed webhooks.
- Exercise the API through HTTP rather than invoking controllers directly.

### Deterministic concurrency tests

Use synchronization barriers rather than timing-based sleeps:

- Two transfers competing for the last balance.
- Many requests sharing one idempotency key.
- Opposing transfers using the same wallets.
- Duplicate webhook deliveries.
- Concurrent settlement finalization.

Every concurrency test asserts both HTTP outcomes and persisted financial invariants.

## Error contract

Use problem-details JSON with stable codes:

- `validation_failed`.
- `authentication_required`.
- `access_denied`.
- `resource_not_found`.
- `insufficient_funds`.
- `currency_mismatch`.
- `wallet_unavailable`.
- `idempotency_key_required`.
- `idempotency_key_conflict`.
- `request_in_progress`.
- `webhook_signature_invalid`.
- `webhook_timestamp_invalid`.
- `reconciliation_conflict`.
- `settlement_period_overlap`.

Map expected domain/application failures centrally. Do not use generic exceptions for ordinary validation results.

## Configuration plan

Use strongly typed, startup-validated options for PostgreSQL, Redis, JWT, webhook secrets and replay tolerance, idempotency retention, reconciliation limits, and settlement fee rules.

Development settings may contain non-sensitive defaults. Secrets come from environment variables, user secrets, CI secrets, or a production secret manager.

## Definition of done for each package

- Code compiles without new warnings.
- Unit and relevant integration tests pass.
- Failure paths are tested alongside happy paths.
- Public endpoints are correct in OpenAPI.
- Logs are useful and exclude sensitive data.
- Database changes include reviewed migrations and constraint tests.
- Documentation changes accompany visible behavior.
- No unrelated refactor is bundled into the package.

## First implementation session

When implementation is authorized, complete work package 0 only:

1. Scaffold the solution and projects.
2. Enforce references and build conventions.
3. Add the API composition root.
4. Add problem details, correlation IDs, logging, OpenAPI, and health routes.
5. Add smoke tests.
6. Run restore, build, and tests.

Do not add users, wallets, or transfers in the same change. A small verified foundation makes later financial logic easier to review.

