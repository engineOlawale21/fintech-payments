using FintechPayments.Domain.Transfers;

namespace FintechPayments.Application.Abstractions.Persistence;

public interface ITransferReader
{
    Task<Transfer?> FindAccessibleAsync(Guid transferId, Guid actorId, CancellationToken cancellationToken);
}
