namespace Ecommerce.Api.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(AreaAvailableId))]
public class AreaAvailable
{
    [Key] public Guid AreaAvailableId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProvinceId { get; set; }
    public bool IsAvailable { get; set; }
    [ForeignKey(nameof(ProductId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Product Product { get; set; } = new();
    [ForeignKey(nameof(ProvinceId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Province Province { get; set; } = new();
}
