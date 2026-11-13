namespace Ecommerce.Api.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(ProductId))]
public class Product
{
    [Key] public Guid ProductId { get; set; }
    [Required] public string ProductName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string ProductImageUrl { get; set; } = string.Empty;
    public bool InStock { get; set; } = true;
    public string Comments { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(CategoryId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Category Category { get; set; } = new();
    public List<UnitPrice> UnitPrices { get; set; } = [];
    public List<AreaAvailable> AreasAvailable { get; set; } = [];
}
