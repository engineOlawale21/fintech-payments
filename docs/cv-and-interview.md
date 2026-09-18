# CV and interview notes

## Portfolio entry

**Fintech Transaction API — ASP.NET Core, C#, EF Core, PostgreSQL**  
Built a secure wallet and transaction-processing API with atomic transfers, double-entry ledger records, durable idempotency, signed webhook deduplication, reconciliation, deterministic settlement, Redis caching, role-based authorization, append-only auditing, automated tests, Docker, and CI.

This is portfolio experience. Do not describe it as professional .NET employment.

## Career-transition explanation

> My background is in backend engineering, where I have worked on APIs, data stores, security, delivery, and production diagnosis. Those reliability problems transfer across languages. I built this C# fintech API to make that transfer concrete: it uses ASP.NET Core and EF Core to implement atomic and idempotent transfers, a double-entry ledger, signed webhooks, reconciliation, settlement, caching, and automated tests. I do not claim prior professional .NET experience; I can demonstrate how quickly I applied existing backend judgment in the .NET ecosystem and explain each tradeoff in the code.

Replace general statements with truthful, measured examples from prior work. Never invent concurrency, revenue, latency, team-size, or leadership figures.

## Interview walkthrough

1. Start with the failure modes: retries, concurrent spending, provider duplication, and mismatched records.
2. Show `TransferExecutor`: deterministic row locks and one PostgreSQL transaction.
3. Explain why money uses `decimal` and fixed-precision PostgreSQL columns.
4. Show the balanced ledger and durable idempotency fingerprint.
5. Demonstrate timestamped webhook HMAC and duplicate replay.
6. Walk from reconciliation discrepancy to deterministic settlement.
7. Explain why Redis is disposable and PostgreSQL remains authoritative.
8. Close with limitations: no cardholder data, no PCI certification, no multi-region writer, and transactional outbox as the next audit/webhook upgrade.
