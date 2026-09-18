namespace FintechPayments.Domain.Reconciliation;

public enum ReconciliationItemCategory
{
    Matched,
    MissingInternal,
    MissingExternal,
    AmountMismatch,
}
