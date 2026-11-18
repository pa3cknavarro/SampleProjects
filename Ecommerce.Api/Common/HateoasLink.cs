// Shared namespace for the hypermedia link type attached to paginated responses.
namespace Ecommerce.Api.Common;

// One link in a HATEOAS payload. The three strings tell a client how to follow
// a related action without hard-coding the URL.
public class HateoasLink
{
    // Relation name such as "self", "next", or "prev". Clients switch on this
    // string to decide what the link means. It starts empty until the endpoint fills it.
    public string Rel { get; set; } = string.Empty;

    // Absolute or relative URL the client should call. Stored as a plain string
    // rather than a Uri so it serializes directly to JSON.
    public string Href { get; set; } = string.Empty;

    // HTTP verb for the link. The default "GET" matches list and self links;
    // a create link can overwrite it with "POST".
    public string Method { get; set; } = "GET";
}
// Ends the link class. A page of results holds a list of these objects.
