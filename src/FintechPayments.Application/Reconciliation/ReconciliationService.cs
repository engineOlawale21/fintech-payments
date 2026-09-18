using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Reconciliation;

namespace FintechPayments.Application.Reconciliation;

public sealed class ReconciliationService(IReconciliationRepository repository, IClock clock)
{
    public async Task<ReconciliationStoreResult> CreateAsync(
        string provider, DateTimeOffset periodStart, DateTimeOffset periodEnd,
        IReadOnlyCollection<ProviderStatementRecord> records, CancellationToken cancellationToken)
    {
        if (periodStart >= periodEnd || records.Count == 0)
        {
            throw new DomainException("The reconciliation statement or period is invalid.");
        }

        ProviderStatementRecord[] external = records
            .Select(x => x.Normalize())
            .OrderBy(x => x.Reference, StringComparer.Ordinal)
            .ToArray();
        if (external.Select(x => x.Reference).Distinct(StringComparer.Ordinal).Count() != external.Length)
        {
            throw new DomainException("Provider statement references must be unique.");
        }
        string checksum = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(external)))).ToLowerInvariant();
        IReadOnlyList<InternalTransferRecord> internalRecords =
            await repository.ListCompletedTransfersAsync(periodStart, periodEnd, cancellationToken);

        Dictionary<string, InternalTransferRecord> internalByReference =
            internalRecords.ToDictionary(x => x.Reference, StringComparer.Ordinal);
        List<ReconciliationItem> items = [];
        foreach (ProviderStatementRecord record in external)
        {
            if (!internalByReference.Remove(record.Reference, out InternalTransferRecord? internalRecord))
            {
                items.Add(ReconciliationItem.Create(record.Reference,
                    ReconciliationItemCategory.MissingInternal, null, record.Amount, record.Currency));
                continue;
            }

            bool matched = internalRecord.Amount == record.Amount &&
                string.Equals(internalRecord.Currency, record.Currency, StringComparison.Ordinal);
            items.Add(ReconciliationItem.Create(record.Reference,
                matched ? ReconciliationItemCategory.Matched : ReconciliationItemCategory.AmountMismatch,
                internalRecord.Amount, record.Amount, record.Currency));
        }

        items.AddRange(internalByReference.Values.Select(record => ReconciliationItem.Create(
            record.Reference, ReconciliationItemCategory.MissingExternal,
            record.Amount, null, record.Currency)));
        ReconciliationRun run = ReconciliationRun.Create(
            provider, periodStart, periodEnd, checksum, clock.UtcNow, items);
        return await repository.StoreOnceAsync(run, cancellationToken);
    }

    public Task<ReconciliationRun?> GetAsync(Guid id, bool includeItems, CancellationToken cancellationToken) =>
        repository.FindAsync(id, includeItems, cancellationToken);

    public async Task<ReconciliationItem?> ResolveAsync(
        Guid runId, Guid itemId, Guid actorId, string note, CancellationToken cancellationToken)
    {
        ReconciliationItem? item = await repository.FindItemAsync(runId, itemId, cancellationToken);
        if (item is null) return null;
        item.Resolve(actorId, note, clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return item;
    }
}

public sealed record ProviderStatementRecord(string Reference, decimal Amount, string Currency)
{
    public ProviderStatementRecord Normalize()
    {
        FintechPayments.Domain.Common.Currency parsedCurrency =
            FintechPayments.Domain.Common.Currency.FromCode(Currency);
        Money money = Money.Create(Amount, parsedCurrency);
        if (string.IsNullOrWhiteSpace(Reference) || Reference.Length > 100)
            throw new DomainException("Provider reference is invalid.");
        return new ProviderStatementRecord(Reference.Trim(), money.Amount, parsedCurrency.Code);
    }
}
