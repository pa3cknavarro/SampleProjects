// Namespace for custom middleware components
namespace Ecommerce.Api.Middleware;

using Ecommerce.Api.Common;

/// <summary>
/// Middleware that enforces presence of a valid API key in the request headers.
/// Implements the "Auth in Headers" requirement.
/// Sample pages and Swagger are excluded so the demo UI can load without a key.
/// </summary>
public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isPublic =
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/samples", StringComparison.OrdinalIgnoreCase) ||
            path == "/" ||
            path.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".css", StringComparison.OrdinalIgnoreCase);

        if (isPublic)
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiConstants.ApiKeyHeaderName, out var extractedApiKey) ||
            extractedApiKey != ApiConstants.ExpectedApiKey)
        {
            var error = new ErrorEnvelope
            {
                TraceId = context.TraceIdentifier,
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = $"A valid '{ApiConstants.ApiKeyHeaderName}' header is required."
            };

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(error);
            return;
        }

        await _next(context);
    }
}
