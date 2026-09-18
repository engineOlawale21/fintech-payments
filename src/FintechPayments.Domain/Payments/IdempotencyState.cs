namespace FintechPayments.Domain.Payments;

public enum IdempotencyState
{
    InProgress = 1,
    Completed = 2,
    FailedFinal = 3,
}
