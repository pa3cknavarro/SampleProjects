// Imports the concurrent collections so the store can use a dictionary that
// allows overlapping requests without an external lock.
using System.Collections.Concurrent;

// Shared namespace. The middleware in Ecommerce.Api.Middleware resolves this type
// from dependency injection and calls TryGet and Set.
namespace Ecommerce.Api.Common;

// In-memory cache of completed idempotent responses. A single instance is
// registered for the process; nothing is written to the database.
public class IdempotencyStore
{
    // Thread-safe map from the client key to the saved response. new() allocates
    // an empty dictionary. ConcurrentDictionary methods are safe if two requests share a key.
    private readonly ConcurrentDictionary<string, IdempotencyEntry> _store = new();

    // How long a saved response stays valid. static readonly means one TimeSpan
    // is shared by every store, and FromHours(24) builds a 24-hour duration.
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    // Looks up a key. The out parameter receives the entry when the method returns
    // true, and null when it returns false. Callers must check the bool before using entry.
    public bool TryGet(string key, out IdempotencyEntry? entry)
    {
        // TryGetValue writes the stored object into existing when the key is present.
        // The && then checks that ExpiresAt is still later than the current UTC time,
        // so an expired hit is treated as a miss.
        if (_store.TryGetValue(key, out var existing) && existing.ExpiresAt > DateTime.UtcNow)
        {
            // Hands the live entry back to the caller through the out parameter.
            entry = existing;
            // True tells the middleware to replay this response and skip the rest of the pipeline.
            return true;
        }
        // The key was missing or expired. TryRemove deletes it if it is still there.
        // The discarded out value is the removed entry, which this method does not need.
        _store.TryRemove(key, out _);
        // A miss is represented by a null entry.
        entry = null;
        // False tells the middleware to run the request and store the result afterward.
        return false;
    }

    // Saves a finished response under the client key, overwriting any previous value
    // for that same key via the dictionary indexer.
    public void Set(string key, int statusCode, string responseBody)
    {
        // Allocates a new entry object. The object initializer below copies the
        // status, body, and expiry onto its properties before the assignment completes.
        _store[key] = new IdempotencyEntry
        {
            // HTTP status that will be replayed on the next identical key.
            StatusCode = statusCode,
            // Raw response body captured as text so the middleware can write it unchanged.
            ResponseBody = responseBody,
            // UtcNow plus 24 hours. After this instant, TryGet treats the entry as expired.
            ExpiresAt = DateTime.UtcNow.Add(DefaultTtl)
        };
    }
}

// Plain data held in the dictionary. It is public so the middleware can read
// StatusCode and ResponseBody from the out parameter of TryGet.
public class IdempotencyEntry
{
    // Status code captured from the original response.
    public int StatusCode { get; set; }

    // Body text captured from the original response. The empty-string initializer
    // keeps the property non-null if an entry is constructed without a body.
    public string ResponseBody { get; set; } = string.Empty;

    // UTC instant after which TryGet discards the entry.
    public DateTime ExpiresAt { get; set; }
}
