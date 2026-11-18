// Nested namespace for enums so order status is not mixed with entity classes.
namespace Ecommerce.Api.Models.Enumerations;

// Named lifecycle states for an order. EF Core stores the underlying int unless
// a conversion is configured, so the database column holds 0 through 4.
public enum OrderStatus
{
    // 0. Also the default for a new OrderStatus field, matching a newly created order.
    Pending = 0,

    // 1. The order has been accepted and is being prepared.
    Processing = 1,

    // 2. The order has left the seller.
    Shipped = 2,

    // 3. The order reached the customer.
    Delivered = 3,

    // 4. The order was stopped and should not be fulfilled.
    Cancelled = 4
}
