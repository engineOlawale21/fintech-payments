# Security policy

## Supported scope

This repository is an educational portfolio project, not a hosted payment service. It does not process real cardholder data, is not PCI-DSS certified, and must not be deployed with development credentials or defaults.

Security fixes target the latest revision of the default branch. No older releases are currently maintained.

## Reporting a vulnerability

Do not disclose a suspected vulnerability in a public issue. Use GitHub's private vulnerability-reporting feature for this repository when available. Include the affected endpoint or component, reproduction steps, potential impact, and any suggested mitigation. Do not include real credentials, access tokens, customer information, or payment data.

## Local-development responsibilities

- Copy `.env.example` to `.env` and replace every placeholder.
- Never commit `.env`, signing keys, webhook secrets, certificates, database dumps, or production data.
- Use synthetic data only.
- Rotate any credential immediately if it is accidentally exposed.
- Apply EF Core migrations explicitly before starting a new application version.
- Treat PostgreSQL as authoritative; Redis is a disposable read cache.

## Production gaps

A real deployment requires managed secret storage and rotation, persistent encrypted data-protection keys, TLS termination, restricted network access, database backups and recovery exercises, centralized audit retention, dependency and container scanning, an outbox-backed event pipeline, alerting, and an independent security review.
