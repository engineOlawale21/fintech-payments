# Database design

## Current schema

The migrations establish the complete transaction-processing model. PostgreSQL is the source of truth; Redis is not used to determine financial correctness.

```mermaid
erDiagram
    USER ||--o{ WALLET : owns
    WALLET ||--o{ LEDGER_ENTRY : records
    LEDGER_TRANSACTION ||--|{ LEDGER_ENTRY : contains
    TRANSFER ||--|| LEDGER_TRANSACTION : posts
    WALLET ||--o{ TRANSFER : source
    WALLET ||--o{ TRANSFER : destination
    USER ||--o{ IDEMPOTENCY_RECORD : owns
    TRANSFER ||--o| IDEMPOTENCY_RECORD : result
    RECONCILIATION_RUN ||--|{ RECONCILIATION_ITEM : contains
    RECONCILIATION_RUN ||--o{ SETTLEMENT_BATCH : sources
    SETTLEMENT_BATCH ||--|{ SETTLEMENT_ITEM : contains
    RECONCILIATION_ITEM ||--o| SETTLEMENT_ITEM : settles
    USER {
        uuid id PK
        varchar email
        varchar normalized_email UK
        varchar password_hash
        varchar role
        varchar status
        timestamptz created_at
    }
    WALLET {
        uuid id PK
        uuid owner_id
        char currency
        numeric available_balance
        varchar status
        bigint version
        timestamptz created_at
        timestamptz updated_at
    }
    LEDGER_TRANSACTION {
        uuid id PK
        varchar type
        varchar reference UK
        timestamptz created_at
    }
    LEDGER_ENTRY {
        uuid id PK
        uuid ledger_transaction_id FK
        uuid wallet_id FK
        varchar direction
        numeric amount
        char currency
        timestamptz created_at
    }
    TRANSFER {
        uuid id PK
        uuid source_wallet_id FK
        uuid destination_wallet_id FK
        numeric amount
        char currency
        varchar reference UK
        varchar status
        timestamptz created_at
        timestamptz updated_at
    }
    IDEMPOTENCY_RECORD {
        uuid id PK
        uuid actor_id FK
        varchar operation
        varchar key
        char request_hash
        varchar state
        uuid transfer_id FK
        integer response_status_code
        jsonb response_body
        timestamptz expires_at
    }
    RECONCILIATION_RUN {
        uuid id PK
        varchar provider
        timestamptz period_start
        timestamptz period_end
        char input_checksum
        numeric internal_total
        numeric external_total
    }
    SETTLEMENT_BATCH {
        uuid id PK
        uuid reconciliation_run_id FK
        char currency
        numeric gross_amount
        numeric fee_amount
        numeric adjustment_amount
        numeric net_amount
        varchar status
    }
    AUDIT_EVENT {
        uuid id PK
        uuid actor_id
        varchar action
        varchar entity_type
        uuid entity_id
        varchar correlation_id
        jsonb metadata
        timestamptz created_at
    }
```

Current guarantees:

- Normalized email is unique and password hashes are never returned through API contracts.
- `numeric(19,4)` stores wallet balances without floating-point rounding.
- A check constraint prevents a negative materialized balance.
- A unique `(owner_id, currency)` index permits one wallet per currency per owner.
- Currency codes are fixed at three characters and validated in the domain.
- `version` is an application-managed concurrency token. Transfer code must increment it when updating a wallet.
- Timestamps use PostgreSQL `timestamp with time zone` and the domain normalizes creation time to UTC.
- Ledger transaction references are unique, entries require positive `numeric(19,4)` amounts, and foreign keys use restricted deletion.
- Wallet ledger reads use the `(wallet_id, created_at, id)` index and keyset cursors rather than offset pagination.
- A completed transfer has one ledger transaction through a unique, restricted `transfer_id` foreign key.
- Transfer execution locks both wallet rows in deterministic UUID order before rechecking funds and posting all changes in one PostgreSQL transaction.
- `(actor_id, operation, key)` uniquely identifies an idempotent request. The request hash detects conflicting reuse, while the stored status and JSON response support deterministic replay after process or cache restarts.
- `(provider, period_start, period_end, input_checksum)` makes identical reconciliation input idempotent.
- Each reconciliation item can appear in at most one settlement through a unique input-item index.
- Settlement finalization uses serializable isolation and rejects overlapping finalized currency periods.
- Audit records are indexed by actor, entity, and correlation ID; a PostgreSQL trigger rejects updates and deletes.

The wallet balance is a transactional projection updated with the immutable ledger in the same transfer transaction.

## Migration commands

Restore the repository-local EF tool:

```bash
dotnet tool restore
```

Create a migration:

```bash
dotnet tool run dotnet-ef migrations add MigrationName \
  --project src/FintechPayments.Infrastructure \
  --startup-project src/FintechPayments.Api \
  --output-dir Persistence/Migrations
```

Apply migrations to the configured PostgreSQL instance:

```bash
dotnet tool run dotnet-ef database update \
  --project src/FintechPayments.Infrastructure \
  --startup-project src/FintechPayments.Api
```

Production startup will not silently apply migrations. Deployment automation must run reviewed migrations explicitly.
