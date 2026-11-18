// Namespace for pipeline components.
namespace Ecommerce.Api.Middleware;

// IdempotencyStore and the header-name constant.
using Ecommerce.Api.Common;

// XML summary. Only unsafe methods with a key are cached; GET, HEAD, and OPTIONS pass through.
/// <summary>
/// Middleware that implements Idempotency-Key support for non-safe HTTP methods.
/// </summary>
// One instance serves every request. The store is injected per call, not stored on the class.
public class IdempotencyMiddleware
{
    // The rest of the pipeline.
    private readonly RequestDelegate _next;

    // Receives the next delegate from the host.
    public IdempotencyMiddleware(RequestDelegate next)
    {
        // Saves it for InvokeAsync.
        _next = next;
    }
    // Ends the constructor.

    // IdempotencyStore is a method parameter, so the DI container resolves a new
    // or shared store for each request depending on how it was registered.
    public async Task InvokeAsync(HttpContext context, IdempotencyStore store)
    {
        // Safe methods are not cached. IsGet, IsHead, and IsOptions compare the verb
        // without caring about letter case. Any true result skips storage.
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            // Continues the pipeline with no replay and no capture.
            await _next(context);
            // Leaves the method.
            return;
        }
        // Ends the safe-method branch.

        // Reads X-Idempotency-Key. If the header is missing, or its first value is
        // null, empty, or whitespace, there is nothing to deduplicate.
        if (!context.Request.Headers.TryGetValue(ApiConstants.IdempotencyKeyHeaderName, out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            // Runs the endpoint normally and does not store the response.
            await _next(context);
            // Leaves the method.
            return;
        }
        // Ends the missing-key branch.

        // First() returns the header value. The ! tells the compiler the value is non-null
        // because the whitespace check above already rejected an empty first value.
        var key = keyValues.First()!;

        // TryGet returns true only for an unexpired entry. The pattern "entry is not null"
        // narrows the nullable out parameter so StatusCode and ResponseBody can be read.
        if (store.TryGet(key, out var entry) && entry is not null)
        {
            // Replays the original status code.
            context.Response.StatusCode = entry.StatusCode;
            // Replays the body as JSON.
            context.Response.ContentType = "application/json";
            // Writes the stored body text and waits for the write.
            await context.Response.WriteAsync(entry.ResponseBody);
            // Does not call the endpoint, so the work is not repeated.
            return;
        }
        // Ends the replay branch.

        // Remembers the real response stream. Later the captured bytes are copied back to it.
        var originalBody = context.Response.Body;
        // A memory stream receives whatever the endpoint writes. using disposes it at the end of the method.
        using var memStream = new MemoryStream();
        // Swaps the response body so downstream writes go to memory instead of the network.
        context.Response.Body = memStream;

        // Runs the endpoint. Its writes land in memStream, and it sets StatusCode on the context.
        await _next(context);

        // Rewinds the memory stream to the first byte so it can be read.
        memStream.Seek(0, SeekOrigin.Begin);
        // Reads the entire captured body into a string. The reader is not disposed explicitly here.
        var bodyText = await new StreamReader(memStream).ReadToEndAsync();

        // Only 2xx responses are stored. Errors are allowed to be retried.
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            // Saves status and body under the client key for the store's TTL.
            store.Set(key, context.Response.StatusCode, bodyText);
        }
        // Ends the success-only store branch.

        // Rewinds again because ReadToEndAsync left the position at the end.
        memStream.Seek(0, SeekOrigin.Begin);
        // Copies the captured bytes to the original network stream.
        await memStream.CopyToAsync(originalBody);
        // Restores the response body so later middleware sees the real stream.
        context.Response.Body = originalBody;
    }
    // Ends InvokeAsync. The using disposes memStream as the method returns.
}
// Ends the middleware class.
