// Entity namespace.
namespace Ecommerce.Api.Models;

// Key, Required, and related validation attributes.
using System.ComponentModel.DataAnnotations;

// ForeignKey attribute used on the category navigation.
using System.ComponentModel.DataAnnotations.Schema;

// PrimaryKey and DeleteBehavior attributes.
using Microsoft.EntityFrameworkCore;

// Single-column key ProductId.
[PrimaryKey(nameof(ProductId))]
// A sellable item. Related prices and availability areas hang off the collection navigations.
public class Product
{
    // Key attribute and the property share one line. EF uses ProductId as the primary key.
    [Key] public Guid ProductId { get; set; }

    // Required name. The empty string keeps the non-nullable property valid before assignment.
    [Required] public string ProductName { get; set; } = string.Empty;

    // Optional marketing text. Not marked Required, but still non-nullable with an empty default.
    public string ProductDescription { get; set; } = string.Empty;

    // Foreign key to Category. A Guid value type cannot be null, so every product row points at a category id.
    public Guid CategoryId { get; set; }

    // URL or path of the product image, stored as text. Empty until set.
    public string ProductImageUrl { get; set; } = string.Empty;

    // Availability flag. The initializer true means a new product is in stock unless code sets it false.
    public bool InStock { get; set; } = true;

    // Free-form notes. Non-nullable, default empty.
    public string Comments { get; set; } = string.Empty;

    // Audit user. Non-nullable, default empty.
    public string CreatedBy { get; set; } = string.Empty;

    // Audit timestamp taken in UTC when the object is constructed.
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Binds the Category navigation to the CategoryId column.
    [ForeignKey(nameof(CategoryId))]
    // Deleting a category does not cascade to products.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // Parent category. new() supplies a non-null placeholder until EF loads the real category.
    public Category Category { get; set; } = new();

    // Prices for this product, one per unit of measure. Empty list by default.
    public List<UnitPrice> UnitPrices { get; set; } = [];

    // Provinces where this product can be sold. Empty list by default.
    public List<AreaAvailable> AreasAvailable { get; set; } = [];
}
// Ends the Product entity.
