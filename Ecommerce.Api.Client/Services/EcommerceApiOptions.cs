// Namespace shared with the typed client.
namespace Ecommerce.Api.Client.Services;

// Settings the client reads when it builds URLs and the API-key header.
public class EcommerceApiOptions
{
    // Configuration section name. GetSection("EcommerceApi") uses this constant
    // so the key is not repeated as a loose string.
    public const string SectionName = "EcommerceApi";

    // Origin of the API, without a trailing path. The default points at a local Kestrel port.
    // TrimEnd is applied later so a trailing slash is not doubled.
    public string BaseUrl { get; set; } = "https://localhost:5001";

    // Value sent as X-Api-Key. The default matches ApiConstants.ExpectedApiKey in the API.
    public string ApiKey { get; set; } = "demo-api-key-12345";

    // URL segment inserted into paths such as api/v1/provinces. Default is the first version.
    public string ApiVersion { get; set; } = "v1";
}
// Ends the options class.
