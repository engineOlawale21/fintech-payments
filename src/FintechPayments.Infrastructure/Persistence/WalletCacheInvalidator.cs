using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FintechPayments.Infrastructure.Persistence;

internal sealed class WalletCacheInvalidator(
    IConnectionMultiplexer redis,
    ILogger<WalletCacheInvalidator> logger)
{
    private static readonly Action<ILogger, Guid, Exception?> InvalidationFailed =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(2103, nameof(InvalidationFailed)),
            "Redis wallet cache invalidation failed for owner {OwnerId}");

    public async Task InvalidateAsync(Guid ownerId, Guid? walletId = null)
    {
        try
        {
            IDatabase database = redis.GetDatabase();
            List<RedisKey> keys = [WalletCacheKeys.OwnerList(ownerId)];
            if (walletId.HasValue) keys.Add(WalletCacheKeys.Wallet(ownerId, walletId.Value));
            await database.KeyDeleteAsync([.. keys]);
        }
        catch (RedisException exception)
        {
            InvalidationFailed(logger, ownerId, exception);
        }
    }
}
