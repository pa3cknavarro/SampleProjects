// Namespace for the wrapper list endpoints return instead of a raw array.
namespace Ecommerce.Api.Common;

// Generic container. T is the item type, so the same class can wrap provinces,
// products, or any other DTO without copying the paging fields.
public class PaginatedResponse<T>
{
    // The rows for the current page. IEnumerable allows any sequence, and
    // Enumerable.Empty<T>() gives a non-null empty sequence before items are assigned.
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

    // Count of matching rows before Skip and Take, so the client can compute
    // how many pages exist even though Items holds only one page.
    public int TotalCount { get; set; }

    // Echo of the requested page number so the client does not have to remember
    // the query it just sent.
    public int Page { get; set; }

    // Echo of the page size actually used for this response.
    public int PageSize { get; set; }

    // Computed property with no setter. When PageSize is 0 it returns 0 to avoid
    // dividing by zero; otherwise it rounds TotalCount / PageSize up to a whole page count.
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    // Hypermedia links for this page. new() creates an empty List so callers can
    // Add self, next, and previous links without a null check.
    public List<HateoasLink> Links { get; set; } = new();
}
// Ends the generic response type. JSON serialization includes TotalPages because it has a getter.
