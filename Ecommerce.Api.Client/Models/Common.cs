namespace Ecommerce.Api.Client.Models;

public class HateoasLink
{
    public string Rel { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
}

public class ErrorEnvelope
{
    public string TraceId { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
    public Dictionary<string, object>? Extensions { get; set; }
}

public class PaginatedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<HateoasLink> Links { get; set; } = new();
}

public class ListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
}
