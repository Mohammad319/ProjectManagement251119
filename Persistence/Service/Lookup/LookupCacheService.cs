using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Persistence.Factory;
using System.Collections.Concurrent;

namespace Persistence.Service.Lookup;

/// <summary>
/// Caches reference/lookup data per tenant to avoid repeated DB round-trips for data that rarely changes.
/// Thread-safe: uses per-key SemaphoreSlim to prevent cache stampede on concurrent requests.
/// Singleton: uses IServiceScopeFactory to safely resolve the scoped IDbContextFactoryTenant.
/// </summary>
public sealed class LookupCacheService(IMemoryCache cache, IServiceScopeFactory scopeFactory)
{
    private static readonly MemoryCacheEntryOptions DefaultOptions =
        new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(20))
            .SetAbsoluteExpiration(TimeSpan.FromHours(2));

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <summary>
    /// Returns cached lookup data for the current tenant.
    /// Builds the result once via <paramref name="queryFactory"/> and caches it.
    /// </summary>
    public async Task<IReadOnlyList<T>> GetAsync<T>(
        string cacheKey,
        Func<ShardingSingleDbContext, Task<List<T>>> queryFactory,
        CancellationToken ct = default) where T : class
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactoryTenant>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var key = $"lookup:{typeof(T).Name}:{cacheKey}:{db.TenantId}";

        if (cache.TryGetValue(key, out IReadOnlyList<T>? cached) && cached is not null)
            return cached;

        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue(key, out cached) && cached is not null)
                return cached;

            var result = await queryFactory(db);
            var readOnly = (IReadOnlyList<T>)result;
            cache.Set(key, readOnly, DefaultOptions);
            return readOnly;
        }
        finally
        {
            sem.Release();
        }
    }

    /// <summary>
    /// Invalidates all cached entries for a specific entity type and tenant.
    /// Call after Create/Update/Delete on that entity type.
    /// </summary>
    public async Task InvalidateAsync<T>(CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactoryTenant>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var prefix = $"lookup:{typeof(T).Name}:";

        foreach (var key in _locks.Keys.Where(k => k.StartsWith(prefix) && k.EndsWith($":{db.TenantId}")))
            cache.Remove(key);
    }
}
