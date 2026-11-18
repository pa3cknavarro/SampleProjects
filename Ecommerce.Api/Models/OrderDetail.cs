// Entity namespace.
namespace Ecommerce.Api.Models;

// Key attribute on the line id.
using System.ComponentModel.DataAnnotations;

// Decimal column types and foreign keys.
using System.ComponentModel.DataAnnotations.Schema;

// PrimaryKey and DeleteBehavior.
using Microsoft.EntityFrameworkCore;

// Surrogate key for one order line.
[PrimaryKey(nameof(Id))]
// A single product line on an order, with its own quantity, price, freight, and discount.
public class OrderDetail
{
    // Primary key for the line.
    [Key] public Guid Id { get; set; }

    // Parent order. Foreign key to Order.OrderId.
    public Guid OrderId { get; set; }

    // Product sold on this line.
    public Guid ProductId { get; set; }

    // Unit of measure for Quantity.
    public Guid UomId { get; set; }

    // Quantity with two decimal places, so partial units can be stored.
    [Column(TypeName = "decimal(18,2)")] public decimal Quantity { get; set; }

    // Price per unit, snapshotted on the line. decimal(18,2).
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }

    // Line amount before freight and discount. The class does not multiply it; callers assign it.
    [Column(TypeName = "decimal(18,2)")] public decimal ExtendedPrice { get; set; }

    // Freight allocated to this line.
    [Column(TypeName = "decimal(18,2)")] public decimal ItemFreight { get; set; }

    // Discount allocated to this line.
    [Column(TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; }

    // Optional line comment. Null when the buyer left no note.
    public string? Comment { get; set; }

    // Product navigation uses ProductId.
    [ForeignKey(nameof(ProductId))]
    // Deleting a product does not cascade to order lines.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // Related product. Placeholder until EF loads it.
    public Product Product { get; set; } = new();

    // Unit navigation uses UomId.
    [ForeignKey(nameof(UomId))]
    // Deleting a unit does not cascade to order lines.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // Related unit. Placeholder until EF loads it.
    public UnitOfMeasure UnitOfMeasure { get; set; } = new();

    // Order navigation uses OrderId.
    [ForeignKey(nameof(OrderId))]
    // Deleting an order does not cascade to lines because of NoAction.
    // The application must delete lines explicitly.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // Parent order. Placeholder until EF loads it.
    public Order Order { get; set; } = new();
}
// Ends the order-line entity.
