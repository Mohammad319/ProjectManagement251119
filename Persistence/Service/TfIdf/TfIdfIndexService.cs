using Application.Feature.TfIdf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Persistence.Factory;
using System.Collections.Concurrent;
using TaskResourceBlueprints.Infrastructure;

namespace Persistence.Service.TfIdf;

public sealed class TfIdfIndexService(
    IMemoryCache cache,
    IDbContextFactory<TaskResourceBlueprintsContext> blueprintFactory,
    IDbContextFactoryTenant tenantFactory) : ITfIdfIndexProvider
{
    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

    private readonly ConcurrentDictionary<string, bool> _activeKeys = new();

    // SemaphoreSlim per cache key — يمنع Cache Stampede عند طلبات متزامنة لنفس TenantId
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<Dictionary<string, double>> GetIdfScoresAsync(int tenantId, CancellationToken ct = default)
    {
        var key = $"tfidf_{tenantId}";

        // Fast path: الكاش جاهز
        if (cache.TryGetValue(key, out Dictionary<string, double>? cached) && cached is not null)
            return cached;

        // Slow path: نبني index واحد فقط حتى لو جاءت N طلبات في نفس اللحظة
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            // Double-check بعد الانتظار — قد يكون أُضيف بالفعل
            if (cache.TryGetValue(key, out cached) && cached is not null)
                return cached;

            var scores = await BuildAsync(tenantId, ct);
            cache.Set(key, scores, CacheOptions);
            _activeKeys.TryAdd(key, true);
            return scores;
        }
        finally
        {
            sem.Release();
        }
    }

    public void InvalidateAll()
    {
        foreach (var key in _activeKeys.Keys)
            cache.Remove(key);
        _activeKeys.Clear();
    }

    // tenantId مُمرَّر صراحةً لضمان استخدام السياق الصحيح
    private async Task<Dictionary<string, double>> BuildAsync(int tenantId, CancellationToken ct)
    {
        var docs = new List<string>();

        await using var blueprintDb = await blueprintFactory.CreateDbContextAsync(ct);
        var blueprintTexts = await blueprintDb.Tasks
            .AsNoTracking()
            .Where(t => t.IsActive && t.NormalizedTextSv != null && t.NormalizedTextSv != string.Empty)
            .Select(t => t.NormalizedTextSv)
            .ToListAsync(ct);
        docs.AddRange(blueprintTexts);

        try
        {
            await using var db = await tenantFactory.CreateDbContextAsync(ct);

            // تحقق صريح: السياق يطابق الـ tenantId المطلوب
            if (db.TenantId != tenantId)
                throw new InvalidOperationException(
                    $"TfIdf build: expected tenant {tenantId} but got {db.TenantId}.");

            var tenantTexts = await db.Tasks
                .AsNoTracking()
                .Where(t => t.NormalizedTextSv != null && t.NormalizedTextSv != string.Empty)
                .Select(t => t.NormalizedTextSv)
                .ToListAsync(ct);
            docs.AddRange(tenantTexts);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch
        {
            // Tenant DB unavailable — blueprint corpus only.
        }

        return ComputeIdf(docs);
    }

    private static Dictionary<string, double> ComputeIdf(List<string> docs)
    {
        if (docs.Count == 0)
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // Count how many documents contain each token (document frequency).
        var df = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var doc in docs)
        {
            var tokens = doc.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var token in tokens)
                df[token] = df.GetValueOrDefault(token) + 1;
        }

        // Sklearn-style smoothed IDF: log((N+1)/(df+1)) + 1
        // Smoothing prevents zero-division and dampens very common tokens.
        double n = docs.Count;
        return df.ToDictionary(
            kv => kv.Key,
            kv => Math.Log((n + 1.0) / (kv.Value + 1.0)) + 1.0,
            StringComparer.OrdinalIgnoreCase);
    }
}
