namespace FintechPayments.Domain.Webhooks;

public enum WebhookEventStatus
{
    Received = 1,
    Processed = 2,
    Failed = 3,
}
