using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Domain.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FintechPayments.Application.Transfers;

public sealed class TransferService(ITransferExecutor executor, ITransferReader reader)
{
    public Task<TransferExecutionResult> CreateAsync(
        Guid actorId,
        Guid sourceWalletId,
        Guid destinationWalletId,
        decimal amount,
        string currency,
        string reference,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        Money money = Money.Create(amount, Currency.FromCode(currency));
        string normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
        string requestHash = ComputeRequestHash(
            sourceWalletId, destinationWalletId, money, reference);
        return executor.ExecuteAsync(
            actorId,
            sourceWalletId,
            destinationWalletId,
            money,
            reference,
            normalizedKey,
            requestHash,
            cancellationToken);
    }

    public Task<FintechPayments.Domain.Transfers.Transfer?> GetAccessibleAsync(
        Guid transferId,
        Guid actorId,
        CancellationToken cancellationToken) =>
        reader.FindAccessibleAsync(transferId, actorId, cancellationToken);

    private static string NormalizeIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100)
        {
            throw new DomainException("Idempotency-Key is required and cannot exceed 100 characters.");
        }

        return key.Trim();
    }

    private static string ComputeRequestHash(
        Guid sourceWalletId,
        Guid destinationWalletId,
        Money money,
        string reference)
    {
        string canonical = string.Join('\n',
            sourceWalletId.ToString("N"),
            destinationWalletId.ToString("N"),
            money.Amount.ToString("0.############################", CultureInfo.InvariantCulture),
            money.Currency.Code,
            reference.Trim());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
