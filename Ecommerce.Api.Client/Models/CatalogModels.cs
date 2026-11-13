namespace Ecommerce.Api.Client.Models;

public class CategoryResponse
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class ProductResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public bool InStock { get; set; }
}
