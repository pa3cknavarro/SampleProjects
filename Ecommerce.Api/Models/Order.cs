// Entity namespace.
namespace Ecommerce.Api.Models;

// Key attribute on OrderId.
using System.ComponentModel.DataAnnotations;

// Column attribute that forces a decimal precision on money fields.
using System.ComponentModel.DataAnnotations.Schema;

// OrderStatus enum used by the status property.
using Ecommerce.Api.Models.Enumerations;

// PrimaryKey and DeleteBehavior.
using Microsoft.EntityFrameworkCore;

// Single-column primary key.
[PrimaryKey(nameof(OrderId))]
// A customer order header. Line items live in OrderDetail.
public class Order
{
    // Primary key for the order header.
    [Key] public Guid OrderId { get; set; }

    // When the order was placed. Defaults to the current UTC time at construction.
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    // Foreign key to Customer. Required Guid, so every order points at a customer id.
    public Guid CustomerId { get; set; }

    public Guid? ShippingProvinceId { get; set; }

    public Province? ShippingProvince { get; set; }

    // Enum property. The initializer sets a new order to Pending, which is the value 0.
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;

    // decimal(18,2) maps to a fixed-precision money column: 18 digits, 2 after the decimal.
    // SubTotal is the sum of line extended prices before freight and discount.
    [Column(TypeName = "decimal(18,2)")] public decimal SubTotal { get; set; }

    // Shipping amount for the whole order, same precision as the other money columns.
    [Column(TypeName = "decimal(18,2)")] public decimal OrderFreight { get; set; }

    // Order-level discount. A decimal defaults to 0 when not initialized.
    [Column(TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; }

    // Amount due. The entity does not compute this; callers assign the number.
    [Column(TypeName = "decimal(18,2)")] public decimal GrandTotal { get; set; }

    // Free-text note. Non-nullable, default empty.
    public string Comment { get; set; } = string.Empty;

    // Audit user who created the order.
    public string CreatedBy { get; set; } = string.Empty;

    // UTC creation time captured at construction.
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Audit user who last changed the order. Empty until an update sets it.
    public string ModifiedBy { get; set; } = string.Empty;

    // UTC modification time. Also initialized at construction, then overwritten on update.
    public DateTime ModifiedOn { get; set; } = DateTime.UtcNow;

    // Customer navigation is bound to CustomerId.
    [ForeignKey(nameof(CustomerId))]
    // Deleting a customer does not cascade-delete orders.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // Related customer. Placeholder until EF loads it.
    public Customer Customer { get; set; } = new();

    // Line items. The property name is singular even though the type is a list.
    // [] allocates an empty list so lines can be added without a null check.
    public List<OrderDetail> OrderDetail { get; set; } = [];
}
// Ends the order header.
