using System.Globalization;
using System.Text;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Ledger;

namespace FintechPayments.Application.Ledger;

public sealed class LedgerService(IWalletRepository wallets, ILedgerRepository ledger)
{
    private const int DefaultPageSize = 25;
    private const int MaximumPageSize = 100;

    public async Task<WalletLedgerResult> GetWalletLedgerAsync(
        Guid walletId,
        Guid ownerId,
        string? cursor,
        int? requestedPageSize,
        CancellationToken cancellationToken)
    {
        if (await wallets.FindOwnedAsync(walletId, ownerId, cancellationToken) is null)
        {
            return WalletLedgerResult.NotFound();
        }

        int pageSize = Math.Clamp(requestedPageSize ?? DefaultPageSize, 1, MaximumPageSize);
        LedgerPosition? before = DecodeCursor(cursor);
        IReadOnlyList<LedgerEntry> rows = await ledger.ListWalletEntriesAsync(
            walletId, before, pageSize + 1, cancellationToken);

        LedgerEntry[] entries = rows.Take(pageSize).ToArray();
        string? nextCursor = rows.Count > pageSize && entries.Length > 0
            ? EncodeCursor(entries[^1])
            : null;

        return WalletLedgerResult.Found(entries, nextCursor);
    }

    private static string EncodeCursor(LedgerEntry entry)
    {
        string value = string.Create(
            CultureInfo.InvariantCulture,
            $"{entry.CreatedAt.UtcTicks}:{entry.Id:N}");
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static LedgerPosition? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            string value = cursor.Replace('-', '+').Replace('_', '/');
            value = value.PadRight(value.Length + ((4 - value.Length % 4) % 4), '=');
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            string[] parts = decoded.Split(':', 2);

            if (parts.Length != 2 ||
                !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks) ||
                !Guid.TryParseExact(parts[1], "N", out Guid entryId))
            {
                throw new FormatException();
            }

            return new LedgerPosition(new DateTimeOffset(ticks, TimeSpan.Zero), entryId);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentOutOfRangeException)
        {
            throw new DomainException("Ledger cursor is invalid.");
        }
    }
}

public sealed record WalletLedgerResult(
    bool Exists,
    IReadOnlyList<LedgerEntry> Entries,
    string? NextCursor)
{
    public static WalletLedgerResult Found(IReadOnlyList<LedgerEntry> entries, string? nextCursor) =>
        new(true, entries, nextCursor);

    public static WalletLedgerResult NotFound() => new(false, [], null);
}
