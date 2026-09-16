using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue is not null)
        {
            return cachedValue;
        }

        var value = await factory();
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration,
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };

        _cache.Set(key, value, cacheOptions);
        _logger.LogDebug("Cache miss for key '{Key}'. Added new item.", key);
        return value;
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Invalidated cache key '{Key}'", key);
    }
}
