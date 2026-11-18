// Namespace for pipeline components.
namespace Ecommerce.Api.Middleware;

// ErrorEnvelope is the JSON shape written for unhandled exceptions.
using Ecommerce.Api.Common;

// XML summary read by documentation tools. The middleware turns any exception
// that escapes later middleware into one JSON body.
/// <summary>
/// Global exception handler that converts unhandled exceptions into a consistent ErrorEnvelope.
/// </summary>
// Registered early in the pipeline so its try surrounds the rest of the request.
public class ExceptionHandlingMiddleware
{
    // The remainder of the pipeline, invoked inside the try.
    private readonly RequestDelegate _next;

    // Logger whose category is this class name. LogError writes the exception and a message.
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    // The host supplies both dependencies when it constructs the middleware.
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        // Saves the next delegate.
        _next = next;
        // Saves the logger.
        _logger = logger;
    }
    // Ends the constructor.

    // Per-request method. Exceptions thrown by awaited code below are caught here.
    public async Task InvokeAsync(HttpContext context)
    {
        // Starts a protected region around the rest of the pipeline.
        try
        {
            // Runs routing and the endpoint. If they throw, execution jumps to catch.
            await _next(context);
        }
        // Ends the try body.
        // Catches any Exception, including subclasses. ex holds the thrown object.
        catch (Exception ex)
        {
            // Writes an error log with the exception and the request trace id as a structured value.
            _logger.LogError(ex, "Unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);

            // Switch expression on the exception type. It produces a tuple of status code and title.
            var (status, title) = ex switch
            {
                // Bad arguments from endpoint code become HTTP 400.
                ArgumentException or ArgumentNullException => (StatusCodes.Status400BadRequest, "Bad Request"),
                // A missing entity thrown as KeyNotFoundException becomes HTTP 404.
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                // An explicit unauthorized throw becomes HTTP 401.
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                // Conflicts, such as a duplicate, become HTTP 409.
                InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict"),
                // Every other exception becomes HTTP 500. The discard pattern _ matches anything left.
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };
            // Ends the switch expression.

            // Builds the JSON body from the tuple and the exception message.
            var error = new ErrorEnvelope
            {
                // Same trace id that was written to the log.
                TraceId = context.TraceIdentifier,
                // Status chosen by the switch.
                Status = status,
                // Title chosen by the switch.
                Title = title,
                // Exception message. This is the raw message, not a stack trace.
                Detail = ex.Message
            };
            // Ends the initializer.

            // Sets the response status to the value chosen above.
            context.Response.StatusCode = status;
            // Forces the content type before writing, in case something earlier changed it.
            context.Response.ContentType = "application/json";
            // Writes the envelope as JSON and completes the response.
            await context.Response.WriteAsJsonAsync(error);
        }
        // Ends the catch. The exception is considered handled, so it does not crash the process.
    }
    // Ends InvokeAsync.
}
// Ends the middleware class.
