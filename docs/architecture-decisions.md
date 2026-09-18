# Architecture decisions

## ADR-001: Layered dependency direction

`Api` and `Infrastructure` depend on `Application`, while domain rules remain independent of ASP.NET Core, EF Core, Redis, and provider SDKs. This costs some interfaces and mapping code but makes financial rules testable without infrastructure.

## ADR-002: PostgreSQL is authoritative

Balances, idempotency, webhook deduplication, reconciliation inputs, and settlement inputs use database transactions and unique constraints. Redis accelerates wallet reads but never authorizes a request or decides whether money moved.

## ADR-003: Double-entry ledger plus balance projection

Each transfer creates equal debit and credit entries in the same transaction as the transfer and wallet balance changes. Materialized balances make reads cheap; the immutable ledger provides the accounting trail.

## ADR-004: Pessimistic wallet locking

Transfers lock both wallet rows using `FOR UPDATE` in deterministic UUID order, then recheck ownership, currency, status, and funds. This is straightforward for a single PostgreSQL writer and prevents overspend; very high contention may justify partitioning or a different account processor.

## ADR-005: Durable idempotency

The key is scoped by actor and operation. A canonical request fingerprint detects conflicting reuse, and the completed outcome survives API restarts. A database uniqueness constraint is the final race arbiter.

## ADR-006: Synchronous webhook persistence

The endpoint authenticates the exact raw body and persists a unique event before acknowledging it. Duplicate delivery is a successful no-op. Higher throughput would use the transactional outbox pattern and asynchronous consumers.

## ADR-007: Explicit settlement arithmetic

The portfolio provider fee is `1.5% of gross + 0.10 per transaction`, rounded away from zero to currency precision. Batches store exact reconciliation item IDs, inputs cannot be reused, and finalization uses serializable isolation.

## ADR-008: Audit availability tradeoff

Audits are append-only in PostgreSQL, correlation-aware, and intentionally exclude sensitive request data. They are appended after application operations and fail safely so a committed payment is not reported as failed. A production implementation should write an outbox record inside the business transaction and project audit events asynchronously.
