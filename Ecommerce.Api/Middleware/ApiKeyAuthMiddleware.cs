// Namespace for request pipeline components registered from Program.cs.
namespace Ecommerce.Api.Middleware;

// ErrorEnvelope and the API-key header constants live in Common.
using Ecommerce.Api.Common;

// XML documentation consumed by tooling. It describes the middleware's job:
// require a valid API key, while letting Swagger and sample pages through.
/// <summary>
/// Middleware that enforces presence of a valid API key in the request headers.
/// Implements the "Auth in Headers" requirement.
/// Sample pages and Swagger are excluded so the demo UI can load without a key.
/// </summary>
// The class is constructed once by the middleware pipeline and reused for every request.
public class ApiKeyAuthMiddleware
{
    // The next component in the pipeline. RequestDelegate is a function that
    // takes an HttpContext and returns a Task. Invoking it continues the request.
    private readonly RequestDelegate _next;

    // The host passes the remainder of the pipeline into this constructor.
    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        // Stores the delegate so InvokeAsync can call it after the key check passes.
        _next = next;
    }
    // Ends the constructor.

    // Entry point called by ASP.NET Core for each request. async Task lets the
    // method await the rest of the pipeline or the JSON error write.
    public async Task InvokeAsync(HttpContext context)
    {
        // Path.Value can be null, so ?? substitutes an empty string. Later
        // comparisons then run on a real string.
        var path = context.Request.Path.Value ?? string.Empty;

        // True when this URL should skip authentication. Each StartsWith or EndsWith
        // is ordinal and case-insensitive. The || chain is true if any one pattern matches.
        var isPublic =
            // Swagger UI and its JSON live under /swagger.
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            // OpenAPI documents may be served under /openapi.
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            // Demo HTML under /samples is allowed without a key.
            path.StartsWith("/samples", StringComparison.OrdinalIgnoreCase) ||
            // The site root is public.
            path == "/" ||
            // Static HTML files are public.
            path.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
            // Static scripts are public.
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            // Static stylesheets are public.
            path.EndsWith(".css", StringComparison.OrdinalIgnoreCase);

        // Public paths skip the header check.
        if (isPublic)
        {
            // Calls the rest of the pipeline and waits until it finishes.
            await _next(context);
            // Returns so the key check below does not also run.
            return;
        }
        // Ends the public-path branch.

        // TryGetValue looks up X-Api-Key without throwing when the header is absent.
        // The second clause rejects a header whose value is not the demo key.
        // StringValues compared with != uses its equality against the expected string.
        if (!context.Request.Headers.TryGetValue(ApiConstants.ApiKeyHeaderName, out var extractedApiKey) ||
            extractedApiKey != ApiConstants.ExpectedApiKey)
        {
            // Builds the JSON body returned for a rejected call.
            var error = new ErrorEnvelope
            {
                // Copies the request's trace id so the client can quote it in a bug report.
                TraceId = context.TraceIdentifier,
                // 401. StatusCodes.Status401Unauthorized is the integer 401.
                Status = StatusCodes.Status401Unauthorized,
                // Short title serialized into the envelope.
                Title = "Unauthorized",
                // Detail includes the header name via interpolation so the client knows what to send.
                Detail = $"A valid '{ApiConstants.ApiKeyHeaderName}' header is required."
            };
            // Ends the object initializer.

            // Sets the HTTP status line to 401 before the body is written.
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            // Serializes error as JSON and writes it to the response. The await completes when the write finishes.
            await context.Response.WriteAsJsonAsync(error);
            // Stops the pipeline. The endpoint never runs.
            return;
        }
        // Ends the unauthorized branch.

        // The header matched. Continue to routing, endpoints, and the rest of the middleware.
        await _next(context);
    }
    // Ends InvokeAsync.
}
// Ends the middleware class.
