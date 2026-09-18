namespace FintechPayments.Domain.Settlements;

public sealed class SettlementItem
{
    private SettlementItem() { }
    private SettlementItem(Guid id, Guid batchId, Guid reconciliationItemId)
    {
        Id = id;
        SettlementBatchId = batchId;
        ReconciliationItemId = reconciliationItemId;
    }

    public Guid Id { get; private set; }
    public Guid SettlementBatchId { get; private set; }
    public Guid ReconciliationItemId { get; private set; }

    internal static SettlementItem Create(Guid batchId, Guid reconciliationItemId) =>
        new(Guid.NewGuid(), batchId, reconciliationItemId);
}
