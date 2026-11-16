// Groups this entity with the other persistence models.
namespace Ecommerce.Api.Models;

// Imports data-annotation attributes such as Key, Required, and Display.
// These attributes are read by validation and by EF Core's convention model.
using System.ComponentModel.DataAnnotations;

// Imports EF Core attributes. PrimaryKey and DeleteBehavior come from this namespace.
using Microsoft.EntityFrameworkCore;

// Tells EF Core the primary key is the ProvinceId property. nameof returns the
// string "ProvinceId" without a magic string, so a rename is caught by the compiler.
[PrimaryKey(nameof(ProvinceId))]
// Entity class mapped to a Provinces table by convention. Each instance is one row.
public class Province
{
    // Marks this property as a key for annotation-based validation and EF conventions.
    [Key]
    // Validation attribute. If the Guid is Guid.Empty, validators can report this message.
    // EF still treats Guid as a non-nullable value type regardless of this attribute.
    [Required(ErrorMessage = "Province Id is required.")]
    // UI label used by tag helpers and scaffolding. It does not change the JSON or column name.
    [Display(Name = "Province Id")]
    // 128-bit identifier. The getter and setter let EF read and write the value when materializing a row.
    public Guid ProvinceId { get; set; }

    // Rejects a missing name during model validation and supplies the error text.
    [Required(ErrorMessage = "Province Name is required.")]
    // Friendly label "Province Name" for UI display.
    [Display(Name = "Province Name")]
    // Non-nullable string. The empty-string initializer means a new Province is not null
    // before the caller assigns a real name, which satisfies the nullable reference type.
    public string ProvinceName { get; set; } = string.Empty;

    // Label for audit display. No Required attribute, but the string is still non-nullable.
    [Display(Name = "Created By")]
    // Who created the row. Starts as "" so it can be saved before a user name is copied in.
    public string CreatedBy { get; set; } = string.Empty;

    // Label for the timestamp field.
    [Display(Name = "Created On")]
    // Format string {0:d} asks display helpers to render the date in short-date form.
    // It does not change how the value is stored.
    [DisplayFormat(DataFormatString = "{0:d}")]
    // Creation timestamp. DateTime.UtcNow runs when the object is constructed, so a new
    // Province records the current UTC time unless the caller overwrites CreatedOn.
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Collection navigation. EF treats this as one province to many cities.
    // The collection expression [] allocates an empty list so Cities is never null.
    public List<City> Cities { get; set; } = [];
}
// Ends the Province entity.
