namespace Ecommerce.Api.Common;

public static class ApiConstants
{
    public const string ApiKeyHeaderName = "X-Api-Key";
    public const string ExpectedApiKey = "demo-api-key-12345";
    public const string IdempotencyKeyHeaderName = "X-Idempotency-Key";
    public const string RateLimitPolicyName = "fixed";
}
