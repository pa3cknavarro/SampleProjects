namespace Ecommerce.Api.Features.Products;

using Ecommerce.Api.Common;

public class ProductResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public bool InStock { get; set; }
    public List<HateoasLink> Links { get; set; } = new();
}

public class CreateProductRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public Guid CategoryId { get; set; }
    public bool InStock { get; set; } = true;
    public string? CreatedBy { get; set; }
}
