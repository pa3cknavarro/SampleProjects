namespace Ecommerce.Api.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(Id))]
public class OrderDetail
{
    [Key] public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UomId { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ExtendedPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ItemFreight { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; }
    public string? Comment { get; set; }
    [ForeignKey(nameof(ProductId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Product Product { get; set; } = new();
    [ForeignKey(nameof(UomId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public UnitOfMeasure UnitOfMeasure { get; set; } = new();
    [ForeignKey(nameof(OrderId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Order Order { get; set; } = new();
}
