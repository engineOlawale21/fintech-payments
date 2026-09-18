using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Domain.Transfers;
using Microsoft.EntityFrameworkCore;

namespace FintechPayments.Infrastructure.Persistence;

internal sealed class TransferReader(PaymentsDbContext context) : ITransferReader
{
    public Task<Transfer?> FindAccessibleAsync(
        Guid transferId,
        Guid actorId,
        CancellationToken cancellationToken) =>
        context.Transfers
            .AsNoTracking()
            .Where(transfer => transfer.Id == transferId)
            .Where(transfer =>
                context.Wallets.Any(wallet =>
                    wallet.OwnerId == actorId &&
                    (wallet.Id == transfer.SourceWalletId || wallet.Id == transfer.DestinationWalletId)))
            .SingleOrDefaultAsync(cancellationToken);
}
