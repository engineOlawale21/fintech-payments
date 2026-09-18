# Security and threat model

## Assets and boundaries

Wallet balances, ledger history, credentials, signing keys, and financial workflow records are protected assets. Clients and provider requests are untrusted. PostgreSQL is authoritative; Redis is only an optional performance layer.

No card number, CVV, or bank credential is accepted or stored. This project demonstrates PCI-DSS-aligned minimization principles but does not claim PCI-DSS certification.

## Threats and controls

| Threat | Implemented control | Remaining limitation |
|---|---|---|
| Credential theft | Platform password hashing; short-lived JWTs with issuer, audience, signature, and lifetime checks | No refresh-token revocation service |
| Horizontal privilege escalation | Ownership predicates plus Customer/Operations/Admin policies | Real-JWT authorization fixtures should be expanded |
| Duplicate payment | Idempotency key, normalized request hash, durable unique record, stored result | Retention cleanup job is pending |
| Concurrent overspend | Deterministic `FOR UPDATE` wallet locking and one transaction | Multi-region writes are out of scope |
| Forged webhook | Exact-body HMAC-SHA256, constant-time comparison, version and replay window | One active mock-provider key |
| Audit tampering | PostgreSQL trigger rejects audit updates and deletes | Audit append is post-operation; use a transactional outbox in production |
| Stale cache | Ownership-scoped keys, 30-second TTL, post-commit invalidation, database fallback | Every future balance writer must invalidate keys |
| Resource exhaustion | Body-size limit and endpoint-specific rate limits | Multi-instance limiting needs a shared gateway/store |

## Data handling

Structured logs and audits may contain entity IDs, action, outcome, correlation ID, timestamp, and controlled error code. They must never contain passwords, JWTs, authorization headers, webhook signatures/secrets, or raw payment payloads.

#### Production follow-ups

- Managed secret storage and webhook key rotation.
- Transactional outbox for business events and audits.
- Ledger mutation protection at the database level.
- Distributed rate limiting and metrics/alerting.
- Backup/restore drills, retention policies, and least-privilege database roles.
