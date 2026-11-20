// Entity namespace.
namespace Ecommerce.Api.Models;

// Key attribute on the surrogate id.
using System.ComponentModel.DataAnnotations;

// Column precision for money and quantity, plus ForeignKey.
using System.ComponentModel.DataAnnotations.Schema;

// PrimaryKey and DeleteBehavior.
using Microsoft.EntityFrameworkCore;

// Surrogate primary key named Id.
[PrimaryKey(nameof(Id))]
// One line in a user's cart: a product, a unit, a price, and a quantity.
public class CartItem
{
    // Primary key for this cart line.
    [Key] public Guid Id { get; set; }

    // Owner of the cart line. Foreign key to User.Id.
    public Guid UserId { get; set; }

    // Product being reserved. Foreign key to Product.ProductId.
    public Guid ProductId { get; set; }

    // Snapshot of the unit price at the time it was added. decimal(18,2) fixes the scale.
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }

    // Unit of measure for the quantity. Foreign key to UnitOfMeasure.Id.
    public Guid UomId { get; set; }

    // How many units. Stored as decimal(18,2) so fractional quantities such as 1.50 are allowed.
    [Column(TypeName = "decimal(18,2)")] public decimal Quantity { get; set; }

    // Optional creator. The ? allows null, and there is no empty-string initializer.
    public string? CreatedBy { get; set; }

    // Optional timestamp. When the object is constructed it becomes UtcNow, but callers may set null.
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;

    // User navigation is bound to UserId.
    [ForeignKey(nameof(UserId))]
    // Deleting a user does not cascade-delete cart lines.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // The required related user assigned by EF Core.
    public User User { get; set; } = null!;

    // Product navigation is bound to ProductId.
    [ForeignKey(nameof(ProductId))]
    // Deleting a product does not cascade-delete cart lines.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // The required related product assigned by EF Core.
    public Product Product { get; set; } = null!;

    // Unit navigation is bound to UomId.
    [ForeignKey(nameof(UomId))]
    // Deleting a unit does not cascade-delete cart lines.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // The required related unit assigned by EF Core.
    public UnitOfMeasure Uom { get; set; } = null!;
}
// Ends the cart-line entity.
