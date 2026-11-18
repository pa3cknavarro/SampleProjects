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
    // Stores the next HTTP pipeline delegate for forwarding approved requests.
    private readonly RequestDelegate _next;

    // Receives the pipeline continuation when ASP.NET constructs middleware.
    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        // Saves the continuation delegate for later invocation.
        _next = next;
    }

    // Processes each incoming request and applies demo API-key enforcement.
    public async Task InvokeAsync(HttpContext context)
    {
        // Allows browser preflight requests to reach CORS middleware without an API key.
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            // Forwards the OPTIONS request to CORS and the remaining pipeline.
            await _next(context);
            // Stops processing after forwarding the preflight.
            return;
        }

        // Captures the normalized request path for public-route checks.
        var path = context.Request.Path.Value ?? string.Empty;
        // Tests the path against the small set of public resources.
        var isPublic =
            // Allows Swagger endpoints to remain browsable.
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            // Allows OpenAPI resources to remain browsable.
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            // Allows demo sample routes.
            path.StartsWith("/samples", StringComparison.OrdinalIgnoreCase) ||
            // Allows the application root.
            path == "/" ||
            // Allows HTML sample assets.
            path.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
            // Allows JavaScript sample assets.
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            // Allows stylesheet sample assets.
            path.EndsWith(".css", StringComparison.OrdinalIgnoreCase);

        // Continues public resources without API-key validation.
        if (isPublic)
        {
            // Sends the public request to the next middleware component.
            await _next(context);
            // Stops this middleware after public-route forwarding.
            return;
        }

        // Rejects requests that omit or mismatch the configured API-key header.
        if (!context.Request.Headers.TryGetValue(ApiConstants.ApiKeyHeaderName, out var extractedApiKey) ||
            extractedApiKey != ApiConstants.ExpectedApiKey)
        {
            // Creates the controlled unauthorized response envelope.
            var error = new ErrorEnvelope
            {
                // Links the error to the current request trace.
                TraceId = context.TraceIdentifier,
                // Uses the standard unauthorized status.
                Status = StatusCodes.Status401Unauthorized,
                // Names the HTTP failure.
                Title = "Unauthorized",
                // Explains the missing or invalid API-key requirement safely.
                Detail = $"A valid '{ApiConstants.ApiKeyHeaderName}' header is required."
            };

            // Sets the unauthorized HTTP status.
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            // Serializes the error envelope as JSON.
            await context.Response.WriteAsJsonAsync(error);
            // Stops the pipeline instead of forwarding an unauthorized request.
            return;
        }

        // Forwards authenticated API requests to rate limiting and endpoint middleware.
        await _next(context);
    }
}
