// Declares the namespace so this type is grouped with other shared API types
// and can be imported as Ecommerce.Api.Common from endpoints and middleware.
namespace Ecommerce.Api.Common;

// Marks the class as static, which means it cannot be instantiated and exists
// only as a container for compile-time constants shared across the API.
public static class ApiConstants
{
    // Defines a compile-time constant for the header name clients must send.
    // The string is baked into the assembly and compared by the API-key middleware.
    public const string ApiKeyHeaderName = "X-Api-Key";

    // The shared secret the middleware accepts for this demo. Because it is a
    // const, every reference is the same string literal and there is no config lookup.
    public const string ExpectedApiKey = "demo-api-key-12345";

    // Header name used by the idempotency middleware. A client repeats this value
    // so a retried POST can return the stored response instead of creating a duplicate.
    public const string IdempotencyKeyHeaderName = "X-Idempotency-Key";

    // Name passed to the rate-limiter policy registered in Program.cs. Endpoints
    // that call RequireRateLimiting use this exact string to select the fixed window.
    public const string RateLimitPolicyName = "fixed";
}
// Closes the static class. No instance state exists beyond these four constants.
