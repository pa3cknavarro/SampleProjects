// Entity namespace.
namespace Ecommerce.Api.Models;

// Key, Required, and Display attributes.
using System.ComponentModel.DataAnnotations;

// Column precision and ForeignKey.
using System.ComponentModel.DataAnnotations.Schema;

// Composite primary key and delete behavior.
using Microsoft.EntityFrameworkCore;

// A price is identified by product, unit, and its own id together.
[PrimaryKey(nameof(ProductId), nameof(UomId), nameof(UnitPriceId))]
// The selling price of one product in one unit of measure.
public class UnitPrice
{
    // Part of the composite key.
    [Key]
    // Validation message for a missing price id.
    [Required(ErrorMessage = "Unit Price Id is required.")]
    // UI caption.
    [Display(Name = "Unit Price Id")]
    // Surrogate portion of the key.
    public Guid UnitPriceId { get; set; }

    // Part of the composite key and foreign key to Product.
    [Key]
    // Validation message for a missing product id.
    [Required(ErrorMessage = "Product Id is required.")]
    // UI caption.
    [Display(Name = "Product Id")]
    // Product this price belongs to.
    public Guid ProductId { get; set; }

    // Part of the composite key and foreign key to UnitOfMeasure.
    [Key]
    // Validation message for a missing unit id.
    [Required(ErrorMessage = "Uom Id is required.")]
    // UI caption.
    [Display(Name = "Uom Id")]
    // Unit this price applies to.
    public Guid UomId { get; set; }

    // The amount is required for validation.
    [Required(ErrorMessage = "Price is required.")]
    // UI caption "Price".
    [Display(Name = "Price")]
    // Money column with 18 digits and 2 decimal places.
    [Column(TypeName = "decimal(18,2)")]
    // The price amount. Initialized to 0 so a new row has a defined value.
    public decimal Price { get; set; } = 0;

    // Product navigation uses ProductId.
    [ForeignKey(nameof(ProductId))]
    // Deleting a product does not cascade to prices.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // The required related product assigned by EF Core.
    public Product Product { get; set; } = null!;

    // Unit navigation uses UomId.
    [ForeignKey(nameof(UomId))]
    // Deleting a unit does not cascade to prices.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // The required related unit assigned by EF Core.
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
}
// Ends the price entity.
