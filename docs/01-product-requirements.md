# Product requirements

## Product statement

The Fintech Transaction API is a portfolio-grade wallet and payment-processing backend. It demonstrates how a C# service preserves financial correctness when clients retry requests, transfers execute concurrently, external providers deliver duplicate webhooks, and provider records disagree with internal records.

The primary audience is a technical recruiter or engineering interviewer. A secondary audience is an engineer evaluating the API locally.

## Version-one objective

A reviewer can run the system locally, authenticate, create and inspect wallets, execute an internal transfer, retry it without duplication, inspect the resulting ledger, submit a signed provider webhook, run reconciliation, and create a settlement batch.

The API must make failures observable and must not leave balances, transfers, or ledger records partially updated.

## Actors and permissions

| Actor | Purpose | Main permissions |
|---|---|---|
| Customer | Owns wallets and transfers funds | Register, log in, create wallets, view owned wallets and ledger entries, transfer from owned wallets |
| Operations | Investigates payment operations | View transfers, process reconciliation, review mismatches, create settlement batches |
| Administrator | Manages the demonstration system | Operations permissions plus user-role and operational oversight |
| Payment provider | Sends machine-to-machine events | Submit correctly signed webhook events only |

Authorization must be checked using both role and resource ownership. Possessing the `Customer` role does not grant access to another customer's wallet.

## Core user journeys

### Journey 1 — authenticate and create wallets

1. A customer registers with an email address and password.
2. The customer logs in and receives a short-lived JWT access token.
3. The customer creates wallets in supported currencies.
4. The customer can inspect only wallets they own.

Acceptance criteria:

- Normalized email addresses are unique.
- Passwords are hashed using the platform password hasher.
- Invalid or expired tokens receive `401 Unauthorized`.
- Insufficient roles receive `403 Forbidden`.
- Access to a wallet owned by another customer is denied without revealing sensitive details.
- A customer cannot create two wallets for the same supported currency.

### Journey 2 — transfer funds safely

1. An authenticated customer submits a source wallet, destination wallet, amount, currency, and `Idempotency-Key`.
2. The API validates ownership, destination, currency, amount, and funds.
3. The API commits the transfer, two balanced ledger entries, and balance changes atomically.
4. The API returns a stable transfer resource.

Acceptance criteria:

- Amounts must be positive and valid for the selected currency.
- Source and destination wallets must differ and use the requested currency.
- Only the source-wallet owner may initiate a transfer.
- A successful transfer contains equal debit and credit postings.
- A wallet cannot have a negative available balance.
- Any failure before commit leaves all related records unchanged.
- Simultaneous requests cannot spend the same funds twice.

### Journey 3 — retry without duplicate payment

1. A client retries a transfer using the original `Idempotency-Key`.
2. The API recognizes the same caller, key, endpoint, and request fingerprint.
3. The API returns the original outcome without creating another transfer.

Acceptance criteria:

- The first request reserves the key before applying the business effect.
- The key and request fingerprint are persisted in PostgreSQL.
- An identical retry returns the recorded status and response.
- Reusing a key for a different payload returns `409 Conflict`.
- Concurrent requests using the same key produce at most one transfer.
- Correctness survives API or Redis restart.

### Journey 4 — process provider webhooks

1. A mock payment provider sends an event with an event ID, timestamp, and HMAC signature.
2. The API verifies the signature against the unmodified body.
3. The event is durably recorded and processed once.
4. Duplicate delivery returns success without repeating its business effect.

Acceptance criteria:

- Missing, invalid, or expired signatures are rejected.
- Signature comparison is constant-time.
- A provider/event-ID pair is unique.
- Payload tampering is detectable.
- Every accepted event records its processing status and failure reason when applicable.
- Logs do not contain the webhook secret or complete signature.

### Journey 5 — reconcile internal and provider records

1. An operations user starts reconciliation for a provider and time period.
2. The service compares a deterministic mock provider statement with internal transfers.
3. It categorizes matches and discrepancies.
4. Operations can inspect the run and individual discrepancies.

Acceptance criteria:

- Categories include matched, missing internally, missing externally, and amount mismatch.
- The input statement has a checksum so the run is reproducible.
- Repeating the same provider, period, and checksum does not create conflicting results.
- Totals equal the sum of persisted reconciliation items.
- Mismatches remain auditable after resolution.

### Journey 6 — create a settlement batch

1. An operations user requests settlement for a closed period and currency.
2. The service selects eligible transactions and computes gross, fees, adjustments, and net.
3. It creates a deterministic batch that can be inspected later.

Acceptance criteria:

- Only eligible, non-settled records are included.
- Finalized settlement periods cannot overlap for the same provider and currency.
- Settlement arithmetic is deterministic and reproducible.
- Finalized settlements are immutable; corrections use adjustments.
- The batch exposes counts and totals without exposing sensitive customer data.

## Financial invariants

These rules are non-negotiable and should be enforced as close to the domain and database as practical:

1. Money is represented with `decimal`, never `float` or `double`.
2. Every ledger transaction balances: total debits equal total credits.
3. Posted ledger entries are immutable.
4. Corrections create compensating entries rather than modifying history.
5. A completed transfer has exactly one ledger transaction.
6. Balance updates and ledger postings commit in the same database transaction.
7. A wallet balance cannot fall below zero in version one.
8. Every externally supplied reference and provider event ID has an explicit uniqueness boundary.
9. Redis is never the sole source of financial truth.
10. Timestamps are stored in UTC.

## Supported version-one rules

- Initial supported currencies: `NGN`, `USD`, and `GBP`.
- One wallet per owner per currency.
- Internal transfers only; the mock provider is used for webhook and reconciliation demonstrations.
- Transfer amounts use up to four decimal places in storage, while currency-specific input precision is validated.
- Funding for demonstrations is performed through an authenticated development fixture or administrative test endpoint that is unavailable outside the Development environment.
- Transfer state begins with `Pending` and finishes as `Completed` or `Failed`; a later milestone may add reversal states.

## API quality requirements

- All routes are versioned under `/api/v1`.
- JSON uses consistent camel-case property names.
- Errors use problem-details responses with a correlation ID.
- Collection endpoints use cursor pagination where ordering can change frequently.
- Every endpoint documents success, validation, authentication, authorization, conflict, and server-error responses in OpenAPI.
- Write operations accept cancellation tokens but do not report cancellation after a database commit as though the transfer failed.

## Operational requirements

- PostgreSQL is the source of truth.
- Redis failure may reduce performance or coordination efficiency but must not corrupt money movement.
- Liveness reports whether the process is alive; readiness checks PostgreSQL and Redis dependencies separately.
- Logs are structured and carry correlation ID, actor ID when known, operation, and stable entity identifiers.
- Secrets, passwords, JWTs, authorization headers, and sensitive payload fields are redacted.
- Database migrations are explicit and reviewed; the production profile does not silently apply destructive migrations.

## Demonstration scenario

The finished repository must support this five-minute walkthrough:

1. Start API, PostgreSQL, and Redis with Docker Compose.
2. Open Swagger and authenticate as a seeded demonstration customer.
3. Inspect two funded wallets.
4. transfer funds and show the two balanced ledger entries.
5. Repeat the request with the same idempotency key and show that no duplicate was created.
6. Submit a signed webhook twice and show a single business effect.
7. Run reconciliation containing one seeded discrepancy.
8. Inspect a settlement batch.
9. Run the automated test suite or show the green CI run.

## Success measures

- Clean clone starts using the documented command.
- The complete demonstration scenario works without manual database editing.
- Automated tests prove rollback, ledger balance, duplicate protection, webhook authentication, and concurrent-spend safety.
- CI builds the API and Docker image and runs unit and integration tests.
- No secrets or misleading claims appear in the repository.
- A reviewer can identify the central C#/.NET concepts within five minutes.

## Deferred enhancements

- Refresh tokens and account recovery.
- Real provider integrations.
- Deposits, withdrawals, refunds, and chargebacks.
- A transactional outbox with a message broker.
- Multi-currency conversion and exchange-rate handling.
- KYC/AML workflows.
- OpenTelemetry traces and production metrics backend.
- Horizontal-scale load testing and performance targets.

## Implemented design choices

The original planning questions were resolved as follows:

1. Version one uses wallet accounts and balanced wallet-to-wallet ledger postings; a broader general ledger is a production extension.
2. Transfers use PostgreSQL row locks acquired in deterministic wallet-ID order to prevent overspending and reduce deadlock risk.
3. Idempotency records are scoped to actor and operation, store a canonical request hash, and persist the completed HTTP outcome.
4. Transfer, webhook, reconciliation, and settlement state transitions are enforced by domain methods and persistence constraints.
5. Provider statements use deterministic API input; settlement fees are `1.5% of gross + 0.10 per transaction`, rounded away from zero to the currency precision.
6. JWT access tokens default to 15 minutes. Development credentials come from local configuration and must not be reused outside local development.
7. Package versions are centrally pinned for .NET 10 and restored in locked mode in CI.

The rationale and tradeoffs are recorded in [architecture decisions](architecture-decisions.md).
