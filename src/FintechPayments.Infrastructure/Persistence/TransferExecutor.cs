using System.Data;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Ledger;
using FintechPayments.Domain.Transfers;
using FintechPayments.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using System.Text.Json;
using FintechPayments.Domain.Payments;
using FintechPayments.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace FintechPayments.Infrastructure.Persistence;

internal sealed class TransferExecutor(
    PaymentsDbContext context,
    IClock clock,
    IOptions<IdempotencyOptions> idempotencyOptions,
    WalletCacheInvalidator cache) : ITransferExecutor
{
    private const string Operation = "create_transfer";

    public async Task<TransferExecutionResult> ExecuteAsync(
        Guid actorId,
        Guid sourceWalletId,
        Guid destinationWalletId,
        Money money,
        string reference,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction databaseTransaction =
            await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        IdempotencyRecord? existing = await context.IdempotencyRecords
            .FromSqlInterpolated(
                $"SELECT * FROM idempotency_records WHERE actor_id = {actorId} AND operation = {Operation} AND key = {idempotencyKey} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return await ResolveExistingAsync(existing, requestHash, cancellationToken);
        }

        Guid firstId = sourceWalletId.CompareTo(destinationWalletId) < 0
            ? sourceWalletId
            : destinationWalletId;
        Guid secondId = firstId == sourceWalletId ? destinationWalletId : sourceWalletId;

        List<Wallet> lockedWallets = await context.Wallets
            .FromSqlInterpolated(
                $"SELECT * FROM wallets WHERE id = {firstId} OR id = {secondId} ORDER BY id FOR UPDATE")
            .ToListAsync(cancellationToken);

        Wallet? source = lockedWallets.SingleOrDefault(wallet => wallet.Id == sourceWalletId);
        Wallet? destination = lockedWallets.SingleOrDefault(wallet => wallet.Id == destinationWalletId);

        if (source is null || destination is null)
        {
            return TransferExecutionResult.Failure("wallet_not_found");
        }

        if (source.OwnerId != actorId)
        {
            return TransferExecutionResult.Failure("source_wallet_not_owned");
        }

        if (source.Status != WalletStatus.Active || destination.Status != WalletStatus.Active)
        {
            return TransferExecutionResult.Failure("wallet_unavailable");
        }

        if (!string.Equals(source.Currency, money.Currency.Code, StringComparison.Ordinal) ||
            !string.Equals(destination.Currency, money.Currency.Code, StringComparison.Ordinal))
        {
            return TransferExecutionResult.Failure("currency_mismatch");
        }

        if (source.AvailableBalance < money.Amount)
        {
            return TransferExecutionResult.Failure("insufficient_funds");
        }

        DateTimeOffset timestamp = clock.UtcNow;
        IdempotencyRecord idempotency = IdempotencyRecord.Reserve(
            actorId,
            Operation,
            idempotencyKey,
            requestHash,
            timestamp,
            TimeSpan.FromHours(idempotencyOptions.Value.RetentionHours));
        Transfer transfer = Transfer.Create(
            sourceWalletId, destinationWalletId, money, reference, timestamp);
        LedgerTransaction ledgerTransaction = LedgerTransaction.CreateTransfer(
            transfer.Id, sourceWalletId, destinationWalletId, money, reference, timestamp);

        source.Debit(money, timestamp);
        destination.Credit(money, timestamp);
        transfer.Complete(timestamp);
        idempotency.Complete(
            transfer.Id,
            JsonSerializer.Serialize(new
            {
                id = transfer.Id,
                sourceWalletId = transfer.SourceWalletId,
                destinationWalletId = transfer.DestinationWalletId,
                amount = transfer.Amount,
                currency = transfer.Currency,
                reference = transfer.Reference,
                status = transfer.Status.ToString(),
                createdAt = transfer.CreatedAt,
                updatedAt = transfer.UpdatedAt,
            }),
            timestamp);
        context.Transfers.Add(transfer);
        context.LedgerTransactions.Add(ledgerTransaction);
        context.IdempotencyRecords.Add(idempotency);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await databaseTransaction.CommitAsync(cancellationToken);
            await Task.WhenAll(
                cache.InvalidateAsync(source.OwnerId, source.Id),
                cache.InvalidateAsync(destination.OwnerId, destination.Id));
            return TransferExecutionResult.Success(transfer);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await databaseTransaction.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
            IdempotencyRecord? concurrentRecord = await context.IdempotencyRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(record =>
                    record.ActorId == actorId &&
                    record.Operation == Operation &&
                    record.Key == idempotencyKey,
                    cancellationToken);

            return concurrentRecord is null
                ? TransferExecutionResult.Failure("transfer_reference_conflict")
                : await ResolveExistingAsync(concurrentRecord, requestHash, cancellationToken);
        }
    }

    private async Task<TransferExecutionResult> ResolveExistingAsync(
        IdempotencyRecord record,
        string requestHash,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return TransferExecutionResult.Failure("idempotency_key_conflict");
        }

        if (record.State != IdempotencyState.Completed || record.TransferId is null)
        {
            return TransferExecutionResult.Failure("request_in_progress");
        }

        Transfer? transfer = await context.Transfers
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == record.TransferId.Value, cancellationToken);
        return transfer is null
            ? TransferExecutionResult.Failure("idempotency_result_unavailable")
            : TransferExecutionResult.Success(transfer, isReplay: true);
    }
}
