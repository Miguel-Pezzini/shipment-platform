using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using ShipmentPlatform.Application.Abstractions;
using ShipmentPlatform.Infrastructure.Options;

namespace ShipmentPlatform.Infrastructure.Caching;

public sealed class JsonDistributedCache(
    IDistributedCache distributedCache,
    IOptions<CacheOptions> options) : ICache
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var payload = await distributedCache.GetAsync(key, cancellationToken);
        if (payload is null || payload.Length == 0)
            return default;

        return JsonSerializer.Deserialize<T>(payload, SerializerOptions);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        var entryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
                ?? TimeSpan.FromMinutes(options.Value.DefaultExpirationMinutes)
        };

        await distributedCache.SetAsync(key, payload, entryOptions, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        distributedCache.RemoveAsync(key, cancellationToken);

    public Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var removals = keys.Select(key => distributedCache.RemoveAsync(key, cancellationToken));
        return Task.WhenAll(removals);
    }
}
