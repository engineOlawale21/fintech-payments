using System.Text.Json;
using FintechPayments.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FintechPayments.Infrastructure.Persistence;

internal sealed class CachedWalletQuery(
    PaymentsDbContext context,
    IConnectionMultiplexer redis,
    ILogger<CachedWalletQuery> logger) : IWalletQuery
{
    private static readonly Action<ILogger, string, Exception?> CacheReadFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2101, nameof(CacheReadFailed)),
            "Redis wallet cache read failed for {CacheKey}");
    private static readonly Action<ILogger, string, Exception?> CacheWriteFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2102, nameof(CacheWriteFailed)),
            "Redis wallet cache write failed for {CacheKey}");
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<WalletReadModel?> FindOwnedAsync(
        Guid walletId, Guid ownerId, CancellationToken cancellationToken)
    {
        string key = WalletCacheKeys.Wallet(ownerId, walletId);
        WalletReadModel? cached = await TryGetAsync<WalletReadModel>(key);
        if (cached is not null) return cached;

        WalletReadModel? wallet = await context.Wallets.AsNoTracking()
            .Where(x => x.Id == walletId && x.OwnerId == ownerId)
            .Select(x => new WalletReadModel(
                x.Id, x.Currency, x.AvailableBalance, x.Status.ToString(), x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        if (wallet is not null) await TrySetAsync(key, wallet);
        return wallet;
    }

    public async Task<IReadOnlyList<WalletReadModel>> ListOwnedAsync(
        Guid ownerId, CancellationToken cancellationToken)
    {
        string key = WalletCacheKeys.OwnerList(ownerId);
        List<WalletReadModel>? cached = await TryGetAsync<List<WalletReadModel>>(key);
        if (cached is not null) return cached;

        List<WalletReadModel> wallets = await context.Wallets.AsNoTracking()
            .Where(x => x.OwnerId == ownerId).OrderBy(x => x.Currency)
            .Select(x => new WalletReadModel(
                x.Id, x.Currency, x.AvailableBalance, x.Status.ToString(), x.CreatedAt))
            .ToListAsync(cancellationToken);
        await TrySetAsync(key, wallets);
        return wallets;
    }

    private async Task<T?> TryGetAsync<T>(string key)
    {
        try
        {
            RedisValue value = await redis.GetDatabase().StringGetAsync(key);
            return value.HasValue ? JsonSerializer.Deserialize<T>((string)value!, JsonOptions) : default;
        }
        catch (RedisException exception)
        {
            CacheReadFailed(logger, key, exception);
            return default;
        }
    }

    private async Task TrySetAsync<T>(string key, T value)
    {
        try
        {
            await redis.GetDatabase().StringSetAsync(
                key, JsonSerializer.Serialize(value, JsonOptions), CacheLifetime);
        }
        catch (RedisException exception)
        {
            CacheWriteFailed(logger, key, exception);
        }
    }
}

internal static class WalletCacheKeys
{
    public static string Wallet(Guid ownerId, Guid walletId) => $"wallet:v1:{ownerId:N}:{walletId:N}";
    public static string OwnerList(Guid ownerId) => $"wallets:v1:{ownerId:N}";
}
