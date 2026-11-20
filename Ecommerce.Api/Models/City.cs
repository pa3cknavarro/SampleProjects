// Groups the city entity with the other models.
namespace Ecommerce.Api.Models;

// Data annotations for Key, Required, Display, and DisplayFormat.
using System.ComponentModel.DataAnnotations;

// Schema annotations. ForeignKey and Column live in this namespace.
using System.ComponentModel.DataAnnotations.Schema;

// EF Core annotations for the composite primary key and delete behavior.
using Microsoft.EntityFrameworkCore;

// Composite key of province plus city. Both values together identify one row,
// which allows the same CityId to exist under different provinces.
[PrimaryKey(nameof(ProvinceId), nameof(CityId))]
// One city or municipality row belonging to a province.
public class City
{
    // Marks ProvinceId as part of the key for data annotations.
    [Key]
    // Validation message if the province id is considered missing.
    [Required(ErrorMessage = "Province Id is required.")]
    // UI label. Does not rename the column.
    [Display(Name = "Province Id")]
    // Foreign key value pointing at Province.ProvinceId. Also half of this entity's primary key.
    public Guid ProvinceId { get; set; }

    // Marks CityId as part of the key.
    [Key]
    // Validation message for a missing city id.
    [Required(ErrorMessage = "City Id is required.")]
    // UI label for the city identifier.
    [Display(Name = "City Id")]
    // Second half of the composite primary key.
    public Guid CityId { get; set; }

    // Name is required during validation.
    [Required(ErrorMessage = "City Name is required.")]
    // UI label "City Name".
    [Display(Name = "City Name")]
    // Display name of the city. Empty string is the non-null default.
    public string CityName { get; set; } = string.Empty;

    // UI label for the postal code. The property itself is not marked Required.
    [Display(Name = "Zip Code")]
    // Postal code stored as text so leading zeros are preserved. Defaults to empty.
    public string ZipCode { get; set; } = string.Empty;

    // UI label for the audit user.
    [Display(Name = "Created By")]
    // Username or id of the creator. Non-nullable, default empty.
    public string CreatedBy { get; set; } = string.Empty;

    // UI label for the audit timestamp.
    [Display(Name = "Created On")]
    // Short-date format for UI display only.
    [DisplayFormat(DataFormatString = "{0:d}")]
    // UTC timestamp captured when the object is new.
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Declares that ProvinceId is the foreign key for the Province navigation.
    [ForeignKey(nameof(ProvinceId))]
    // NoAction tells the database not to cascade-delete cities when a province is deleted.
    // The application must remove or reassign children first.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // The required related province assigned by EF Core.
    public Province Province { get; set; } = null!;

    // Children are not cascade-deleted when this city is removed.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // One city has many barangays. [] creates an empty list so the collection can be added to.
    public List<Barangay> Barangays { get; set; } = [];
}
// Ends the City entity.
