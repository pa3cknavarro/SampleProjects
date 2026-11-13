namespace Ecommerce.Api.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ecommerce.Api.Models.Enumerations;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(OrderId))]
public class Order
{
    [Key] public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public Guid CustomerId { get; set; }
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
    [Column(TypeName = "decimal(18,2)")] public decimal SubTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal OrderFreight { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal GrandTotal { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string ModifiedBy { get; set; } = string.Empty;
    public DateTime ModifiedOn { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(CustomerId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Customer Customer { get; set; } = new();
    public List<OrderDetail> OrderDetail { get; set; } = [];
}
