namespace Ecommerce.Api.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(Id))]
public class CartItem
{
    [Key] public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    public Guid UomId { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Quantity { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public User User { get; set; } = new();
    [ForeignKey(nameof(ProductId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Product Product { get; set; } = new();
    [ForeignKey(nameof(UomId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public UnitOfMeasure Uom { get; set; } = new();
}
