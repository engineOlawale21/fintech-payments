# API examples

Base URL: `http://localhost:8080`. OpenAPI is at `/openapi/v1.json` in Development.

## Register and create a wallet

```bash
curl -X POST http://localhost:8080/api/v1/auth/register -H "Content-Type: application/json" -d '{"email":"customer@example.test","password":"CorrectHorse-2026"}'
curl -X POST http://localhost:8080/api/v1/wallets -H "Authorization: Bearer TOKEN" -H "Content-Type: application/json" -d '{"currency":"USD"}'
```

New registrations receive the Customer role. The repository ships no default administrator password.

For local operations testing, explicitly promote a registered account and log in again:

```bash
docker compose exec postgres psql -U fintech -d fintech_payments -c "UPDATE users SET role = 'Operations' WHERE normalized_email = 'OPS@EXAMPLE.TEST';"
```

## Idempotent transfer

Both wallets must use the same currency and the source must have sufficient funds.

```bash
curl -X POST http://localhost:8080/api/v1/transfers -H "Authorization: Bearer TOKEN" -H "Idempotency-Key: checkout-0001" -H "Content-Type: application/json" -d '{"sourceWalletId":"SOURCE_ID","destinationWalletId":"DESTINATION_ID","amount":25.50,"currency":"USD","reference":"order-0001"}'
```

An identical retry returns the same transfer and `Idempotency-Replayed: true`; conflicting key reuse returns `409`.

## Signed webhook (PowerShell)

```powershell
$body = '{"eventId":"513ff180-9cb1-43a0-9ac8-e328e33b6d16","eventType":"payment.settled","resourceReference":"payment-1","amount":25.50,"currency":"USD"}'
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds().ToString([Globalization.CultureInfo]::InvariantCulture)
$secret = 'development-webhook-secret-change-me'
$hmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($secret))
$signature = (($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes("$timestamp.$body")) | ForEach-Object { $_.ToString('x2') }) -join '')
$headers = @{'X-Webhook-Timestamp'=$timestamp; 'X-Webhook-Signature'=$signature; 'X-Webhook-Signature-Version'='v1'}
Invoke-RestMethod -Method Post -Uri http://localhost:8080/api/v1/webhooks/payments/mock-provider -Headers $headers -ContentType application/json -Body $body
```

Repeating the exact event returns `duplicate: true` with the original persisted event ID.

## Reconciliation, settlement, and audit

These calls require an Operations or Admin token.

```bash
curl -X POST http://localhost:8080/api/v1/reconciliations -H "Authorization: Bearer OPERATIONS_TOKEN" -H "Content-Type: application/json" -d '{"provider":"mock-provider","periodStart":"2026-09-01T00:00:00Z","periodEnd":"2026-10-01T00:00:00Z","records":[{"reference":"order-0001","amount":25.50,"currency":"USD"}]}'
curl -X POST http://localhost:8080/api/v1/settlements -H "Authorization: Bearer OPERATIONS_TOKEN" -H "Content-Type: application/json" -d '{"reconciliationRunId":"RUN_ID","currency":"USD","adjustmentAmount":0}'
curl -X POST http://localhost:8080/api/v1/settlements/SETTLEMENT_ID/finalize -H "Authorization: Bearer OPERATIONS_TOKEN"
curl "http://localhost:8080/api/v1/audit-events?pageSize=50" -H "Authorization: Bearer OPERATIONS_TOKEN"
```

Settlement requires matched, previously unsettled transfer records. Errors use Problem Details with a stable `code`. Responses include `X-Correlation-ID`; callers may supply a value up to 128 characters.
