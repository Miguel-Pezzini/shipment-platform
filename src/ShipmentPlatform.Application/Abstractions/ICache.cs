namespace ShipmentPlatform.Application.Abstractions;

public interface ICache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
}
