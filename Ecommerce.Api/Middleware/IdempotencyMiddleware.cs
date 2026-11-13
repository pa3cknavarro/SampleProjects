namespace Ecommerce.Api.Middleware;

using Ecommerce.Api.Common;

/// <summary>
/// Middleware that implements Idempotency-Key support for non-safe HTTP methods.
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IdempotencyStore store)
    {
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiConstants.IdempotencyKeyHeaderName, out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            await _next(context);
            return;
        }

        var key = keyValues.First()!;

        if (store.TryGet(key, out var entry) && entry is not null)
        {
            context.Response.StatusCode = entry.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(entry.ResponseBody);
            return;
        }

        var originalBody = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await _next(context);

        memStream.Seek(0, SeekOrigin.Begin);
        var bodyText = await new StreamReader(memStream).ReadToEndAsync();

        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            store.Set(key, context.Response.StatusCode, bodyText);
        }

        memStream.Seek(0, SeekOrigin.Begin);
        await memStream.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }
}
