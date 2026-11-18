// Namespace for the query object that list endpoints bind from the query string.
namespace Ecommerce.Api.Common;

// A plain class whose public properties match query keys such as ?page=1&pageSize=10.
// ASP.NET Core model binding sets each property from the request when the name matches.
public class PaginationParams
{
    // 1-based page index. The default of 1 means a request that omits ?page=
    // still asks for the first page instead of page 0.
    public int Page { get; set; } = 1;

    // Number of rows to return. Default 10 limits an unbounded query when the
    // client does not send pageSize.
    public int PageSize { get; set; } = 10;

    // Optional free-text filter. The ? makes the string nullable so a missing
    // search parameter binds as null and endpoints can skip the filter.
    public string? Search { get; set; }

    // Optional property name the endpoint uses in an OrderBy. Null means the
    // endpoint keeps its own default sort.
    public string? SortBy { get; set; }

    // Sort direction. The default "asc" is applied when the query string omits
    // sortOrder, so ordering is ascending unless the client sends "desc".
    public string? SortOrder { get; set; } = "asc";
}
// Ends the parameter object. It is not an entity and is not stored in the database.
