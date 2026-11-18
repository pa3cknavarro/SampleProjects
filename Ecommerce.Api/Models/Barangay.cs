// Entity namespace.
namespace Ecommerce.Api.Models;

// Validation attributes for the three key parts and the name.
using System.ComponentModel.DataAnnotations;

// ForeignKey attributes for province and city.
using System.ComponentModel.DataAnnotations.Schema;

// Composite PrimaryKey and DeleteBehavior.
using Microsoft.EntityFrameworkCore;

// Three-part key. A barangay is identified by province, city, and its own id together.
[PrimaryKey(nameof(ProvinceId), nameof(CityId), nameof(BarangayId))]
// The smallest location in this model: a barangay inside a city inside a province.
public class Barangay
{
    // First key component and foreign key to Province.
    [Key]
    // Validation message for a missing province id.
    [Required(ErrorMessage = "Province Id is required.")]
    // UI caption.
    [Display(Name = "Province Id")]
    // Province part of the composite key.
    public Guid ProvinceId { get; set; }

    // Second key component and foreign key to City.
    [Key]
    // Validation message for a missing city id.
    [Required(ErrorMessage = "City Id is required.")]
    // UI caption.
    [Display(Name = "City Id")]
    // City part of the composite key. Together with ProvinceId it matches City's key.
    public Guid CityId { get; set; }

    // Third key component, unique only within the city.
    [Key]
    // Validation message for a missing barangay id.
    [Required(ErrorMessage = "Barangay Id is required.")]
    // UI caption.
    [Display(Name = "Barangay Id")]
    // Barangay part of the composite key.
    public Guid BarangayId { get; set; }

    // The name is required.
    [Required(ErrorMessage = "Barangay Name is required.")]
    // UI caption.
    [Display(Name = "Barangay Name")]
    // Display name. Non-nullable, default empty.
    public string BarangayName { get; set; } = string.Empty;

    // UI caption for the creator.
    [Display(Name = "Created By")]
    // Audit user. Non-nullable, default empty.
    public string CreatedBy { get; set; } = string.Empty;

    // UI caption for the timestamp.
    [Display(Name = "Created On")]
    // Short-date display format.
    [DisplayFormat(DataFormatString = "{0:d}")]
    // UTC timestamp captured when the object is constructed.
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Province navigation uses ProvinceId. CityId is not part of this foreign key.
    [ForeignKey(nameof(ProvinceId))]
    // Deleting the province does not cascade to barangays.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // Related province. Placeholder until EF loads it.
    public Province Province { get; set; } = new();

    // City navigation uses CityId. ProvinceId is a separate foreign key above.
    [ForeignKey(nameof(CityId))]
    // Deleting the city does not cascade to barangays.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // Related city. Placeholder until EF loads it.
    public City City { get; set; } = new();
}
// Ends the barangay entity.
