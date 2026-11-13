namespace Ecommerce.Api.Client.Services;

public class EcommerceApiOptions
{
    public const string SectionName = "EcommerceApi";
    public string BaseUrl { get; set; } = "https://localhost:7001";
    public string ApiKey { get; set; } = "demo-api-key-12345";
}
