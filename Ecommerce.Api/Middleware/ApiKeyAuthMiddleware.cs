// Places the middleware in the API pipeline namespace.
namespace Ecommerce.Api.Middleware;

// Imports the shared API-key name and error-envelope contract.
using Ecommerce.Api.Common;

// Documents middleware behavior for API maintainers.
/// <summary>
/// Requires the configured API key for protected routes and permits public documentation resources.
/// </summary>
// Implements API-key validation for each incoming request.
public class ApiKeyAuthMiddleware
{
    // Stores the next pipeline delegate so approved requests can continue.
    private readonly RequestDelegate _next;

    // Receives the remainder of the ASP.NET Core pipeline.
    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        // Saves the delegate for request-time invocation.
        _next = next;
    }

    // Validates each request before it reaches protected endpoints.
    public async Task InvokeAsync(HttpContext context)
    {
        // Allows CORS preflight requests to reach the CORS middleware without credentials.
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            // Forwards the preflight request to the next middleware.
            await _next(context);
            // Prevents the API-key check from running on this preflight.
            return;
        }

        // Captures the current path for public-resource checks.
        var path = context.Request.Path.Value ?? string.Empty;
        // Identifies documentation and static resources that are intentionally public.
        var isPublic =
            // Allows Swagger UI and OpenAPI JSON resources.
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            // Allows separately hosted OpenAPI resources.
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            // Allows API sample pages.
            path.StartsWith("/samples", StringComparison.OrdinalIgnoreCase) ||
            // Allows the application root.
            path == "/" ||
            // Allows static HTML pages.
            path.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
            // Allows static JavaScript assets.
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            // Allows static CSS assets.
            path.EndsWith(".css", StringComparison.OrdinalIgnoreCase);

        // Skips authentication for paths marked public.
        if (isPublic)
        {
            // Continues processing the public request.
            await _next(context);
            // Stops this middleware after public-route forwarding.
            return;
        }

        // Rejects requests that omit the key or provide a different value.
        if (!context.Request.Headers.TryGetValue(ApiConstants.ApiKeyHeaderName, out var extractedApiKey) ||
            extractedApiKey != ApiConstants.ExpectedApiKey)
        {
            // Constructs a safe error response for unauthenticated callers.
            var error = new ErrorEnvelope
            {
                // Associates the error with this request's trace identifier.
                TraceId = context.TraceIdentifier,
                // Reports the standard unauthorized status.
                Status = StatusCodes.Status401Unauthorized,
                // Provides a concise error title.
                Title = "Unauthorized",
                // Names the required header without revealing secrets.
                Detail = $"A valid '{ApiConstants.ApiKeyHeaderName}' header is required."
            };

            // Sets the response status before writing the envelope.
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            // Serializes the structured error as JSON.
            await context.Response.WriteAsJsonAsync(error);
            // Stops processing so the protected endpoint cannot execute.
            return;
        }

        // Continues processing when the API key is valid.
        await _next(context);
    }
}
