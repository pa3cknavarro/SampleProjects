namespace Ecommerce.Api.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(ProductId), nameof(UomId), nameof(UnitPriceId))]
public class UnitPrice
{
    [Key]
    [Required(ErrorMessage = "Unit Price Id is required.")]
    [Display(Name = "Unit Price Id")]
    public Guid UnitPriceId { get; set; }

    [Key]
    [Required(ErrorMessage = "Product Id is required.")]
    [Display(Name = "Product Id")]
    public Guid ProductId { get; set; }

    [Key]
    [Required(ErrorMessage = "Uom Id is required.")]
    [Display(Name = "Uom Id")]
    public Guid UomId { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Display(Name = "Price")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; } = 0;

    [ForeignKey(nameof(ProductId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Product Product { get; set; } = new();

    [ForeignKey(nameof(UomId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public UnitOfMeasure UnitOfMeasure { get; set; } = new();
}
