namespace FintechPayments.Domain.Ledger;

public enum LedgerTransactionType
{
    Transfer = 1,
    Funding = 2,
    Reversal = 3,
    Adjustment = 4,
}
