// Places the error shape in the shared Common namespace so middleware and
// endpoints can return the same JSON structure.
namespace Ecommerce.Api.Common;

// A mutable class serialized to JSON when a request fails. Each property becomes
// a camel-cased field under the default ASP.NET Core JSON options.
public class ErrorEnvelope
{
    // Correlates the error with server logs. Callers usually copy
    // HttpContext.TraceIdentifier into this string before writing the response.
    public string TraceId { get; set; } = string.Empty;

    // HTTP status code stored as an integer so the body repeats the status line.
    // The initializer is omitted, so a new envelope starts at 0 until code sets it.
    public int Status { get; set; }

    // Short, stable summary such as "Not Found". It is non-nullable and starts
    // as an empty string so serialization never emits null for this field.
    public string Title { get; set; } = string.Empty;

    // Longer explanation of this particular failure. Empty until the throwing
    // code or the exception middleware assigns a message.
    public string Detail { get; set; } = string.Empty;

    // Optional map of field name to validation messages. The nullable reference
    // means the property is omitted or null when there are no field errors.
    public Dictionary<string, string[]>? Errors { get; set; }

    // Optional bag for extra problem-details data. Values are object so callers
    // can attach numbers, strings, or nested objects without a fixed schema.
    public Dictionary<string, object>? Extensions { get; set; }
}
// Ends the class. Instances are created per failed request and not stored.
