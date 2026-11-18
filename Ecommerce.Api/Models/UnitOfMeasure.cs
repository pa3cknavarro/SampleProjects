// EF Core primary-key attribute.
using Microsoft.EntityFrameworkCore;

// Validation and display attributes.
using System.ComponentModel.DataAnnotations;

// Entity namespace.
namespace Ecommerce.Api.Models;

// Primary key property is named Id, not UnitOfMeasureId.
[PrimaryKey(nameof(Id))]
// A unit such as piece, kilogram, or box. Prices and order lines refer to this row.
public class UnitOfMeasure
{
    // Marks Id as the key.
    [Key]
    // Validation message for a missing id.
    [Required(ErrorMessage = "Id cannot be empty.")]
    // UI caption "Id".
    [Display(Name = "Id")]
    // Primary key.
    public Guid Id { get; set; }

    // The unit code is required.
    [Required(ErrorMessage = "UOM cannot be empty.")]
    // UI caption "UOM".
    [Display(Name = "UOM")]
    // Short code such as "pc" or "kg". Non-nullable, default empty.
    public string Uom { get; set; } = string.Empty;

    // Description is required.
    [Required(ErrorMessage = "Description cannot be empty.")]
    // UI caption.
    [Display(Name = "Description")]
    // Human-readable name of the unit. Non-nullable, default empty.
    public string Description { get; set; } = string.Empty;

    // UI caption for the creator.
    [Display(Name = "Created By")]
    // Audit user. Non-nullable, default empty.
    public string CreatedBy { get; set; } = string.Empty;

    // UI caption for the timestamp.
    [Display(Name = "Created On")]
    // Short-date format, also applied in edit mode.
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
    // UTC timestamp captured when the object is constructed.
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
// Ends the unit-of-measure entity.
