using System.Collections.Concurrent;

namespace Ecommerce.Api.Common;

public class IdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyEntry> _store = new();
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    public bool TryGet(string key, out IdempotencyEntry? entry)
    {
        if (_store.TryGetValue(key, out var existing) && existing.ExpiresAt > DateTime.UtcNow)
        {
            entry = existing;
            return true;
        }
        _store.TryRemove(key, out _);
        entry = null;
        return false;
    }

    public void Set(string key, int statusCode, string responseBody)
    {
        _store[key] = new IdempotencyEntry
        {
            StatusCode = statusCode,
            ResponseBody = responseBody,
            ExpiresAt = DateTime.UtcNow.Add(DefaultTtl)
        };
    }
}

public class IdempotencyEntry
{
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
