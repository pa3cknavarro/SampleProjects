namespace Ecommerce.Api.Common;

public class ErrorEnvelope
{
    public string TraceId { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
    public Dictionary<string, object>? Extensions { get; set; }
}
