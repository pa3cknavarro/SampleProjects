// Entity namespace.
namespace Ecommerce.Api.Models;

// Key attribute applied to AreaAvailableId.
using System.ComponentModel.DataAnnotations;

// ForeignKey attributes for product and province.
using System.ComponentModel.DataAnnotations.Schema;

// PrimaryKey and DeleteBehavior.
using Microsoft.EntityFrameworkCore;

// Surrogate key. The pair of ProductId and ProvinceId is not declared as the key here.
[PrimaryKey(nameof(AreaAvailableId))]
// A row that says whether one product is offered in one province.
public class AreaAvailable
{
    // Primary key for this availability row.
    [Key] public Guid AreaAvailableId { get; set; }

    // Product this availability row belongs to.
    public Guid ProductId { get; set; }

    // Province this availability row belongs to.
    public Guid ProvinceId { get; set; }

    // True when the product can be sold in that province. A bool defaults to false
    // when the initializer is omitted, so a new row is unavailable until set.
    public bool IsAvailable { get; set; }

    // Navigation uses ProductId as its foreign key.
    [ForeignKey(nameof(ProductId))]
    // Removing a product does not automatically delete these rows.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // The related product. Placeholder instance until EF assigns the tracked one.
    public Product Product { get; set; } = new();

    // Navigation uses ProvinceId as its foreign key.
    [ForeignKey(nameof(ProvinceId))]
    // Removing a province does not automatically delete these rows.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // The related province. Placeholder instance until EF assigns the tracked one.
    public Province Province { get; set; } = new();
}
// Ends the availability entity.
