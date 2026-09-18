param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$WebhookSecret = "development-webhook-secret-change-me"
)

$ErrorActionPreference = "Stop"
$suffix = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$password = "CorrectHorse-$suffix!"

function Invoke-Api {
    param([string]$Method, [string]$Path, [object]$Body, [string]$Token, [hashtable]$Headers)
    $requestHeaders = @{}
    if ($Token) { $requestHeaders.Authorization = "Bearer $Token" }
    if ($Headers) { foreach ($key in $Headers.Keys) { $requestHeaders[$key] = $Headers[$key] } }
    $parameters = @{ Method = $Method; Uri = "$BaseUrl$Path"; Headers = $requestHeaders }
    if ($null -ne $Body) {
        $parameters.ContentType = "application/json"
        $parameters.Body = if ($Body -is [string]) { $Body } else { $Body | ConvertTo-Json -Depth 8 -Compress }
    }
    Invoke-RestMethod @parameters
}

$health = Invoke-Api Get "/health/ready"
if ($health.status -ne "Healthy") { throw "API is not ready." }

$opsEmail = "ops-$suffix@example.test"
$customerEmail = "customer-$suffix@example.test"
$recipientEmail = "recipient-$suffix@example.test"
$ops = Invoke-Api Post "/api/v1/auth/register" @{ email = $opsEmail; password = $password }
$customer = Invoke-Api Post "/api/v1/auth/register" @{ email = $customerEmail; password = $password }
$recipient = Invoke-Api Post "/api/v1/auth/register" @{ email = $recipientEmail; password = $password }

$normalizedOpsEmail = $opsEmail.ToUpperInvariant()
docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U fintech -d fintech_payments -c "UPDATE users SET role = 'Operations' WHERE normalized_email = '$normalizedOpsEmail';" | Out-Null
$ops = Invoke-Api Post "/api/v1/auth/login" @{ email = $opsEmail; password = $password }

$source = Invoke-Api Post "/api/v1/wallets" @{ currency = "USD" } $customer.accessToken
$destination = Invoke-Api Post "/api/v1/wallets" @{ currency = "USD" } $recipient.accessToken

$transferRejected = $false
try {
    Invoke-Api Post "/api/v1/transfers" @{
        sourceWalletId = $source.id; destinationWalletId = $destination.id
        amount = 10.00; currency = "USD"; reference = "demo-$suffix"
    } $customer.accessToken @{ "Idempotency-Key" = "demo-$suffix" } | Out-Null
} catch {
    $transferRejected = [int]$_.Exception.Response.StatusCode -eq 422
}
if (-not $transferRejected) { throw "Expected insufficient-funds protection." }

$eventId = [Guid]::NewGuid()
$webhookBody = @{ eventId = $eventId; eventType = "payment.settled"; resourceReference = "provider-$suffix"; amount = 10.00; currency = "USD" } | ConvertTo-Json -Compress
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds().ToString([Globalization.CultureInfo]::InvariantCulture)
$hmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($WebhookSecret))
$signature = (($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes("$timestamp.$webhookBody")) | ForEach-Object { $_.ToString("x2") }) -join "")
$webhookHeaders = @{ "X-Webhook-Timestamp" = $timestamp; "X-Webhook-Signature" = $signature; "X-Webhook-Signature-Version" = "v1" }
$firstWebhook = Invoke-Api Post "/api/v1/webhooks/payments/mock-provider" $webhookBody $null $webhookHeaders
$secondWebhook = Invoke-Api Post "/api/v1/webhooks/payments/mock-provider" $webhookBody $null $webhookHeaders
if ($firstWebhook.duplicate -or -not $secondWebhook.duplicate -or $firstWebhook.eventId -ne $secondWebhook.eventId) {
    throw "Webhook deduplication assertion failed."
}

$periodStart = [DateTimeOffset]::UtcNow.AddMinutes(-5).ToString("o")
$periodEnd = [DateTimeOffset]::UtcNow.AddMinutes(5).ToString("o")
$run = Invoke-Api Post "/api/v1/reconciliations" @{
    provider = "mock-provider"; periodStart = $periodStart; periodEnd = $periodEnd
    records = @(@{ reference = "provider-$suffix"; amount = 10.00; currency = "USD" })
} $ops.accessToken
$items = Invoke-Api Get "/api/v1/reconciliations/$($run.id)/items" $null $ops.accessToken
$settlementRejected = $false
try {
    Invoke-Api Post "/api/v1/settlements" @{
        reconciliationRunId = $run.id; currency = "USD"; adjustmentAmount = 0
    } $ops.accessToken | Out-Null
} catch {
    $settlementRejected = [int]$_.Exception.Response.StatusCode -eq 409
}
if (-not $settlementRejected) { throw "Expected settlement input protection." }
$audits = Invoke-Api Get "/api/v1/audit-events?pageSize=20" $null $ops.accessToken

[pscustomobject]@{
    Ready = $health.status
    TransferInsufficientFundsProtected = $transferRejected
    WebhookDuplicateProtected = $secondWebhook.duplicate
    ReconciliationCategory = $items[0].category
    SettlementWithoutMatchedInputsRejected = $settlementRejected
    AuditEventsReturned = @($audits).Count
} | Format-List
