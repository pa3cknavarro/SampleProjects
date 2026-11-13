namespace Ecommerce.Api.Features.Categories;

using Ecommerce.Api.Common;

public class CategoryResponse
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public List<HateoasLink> Links { get; set; } = new();
}

public class CreateCategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
}
