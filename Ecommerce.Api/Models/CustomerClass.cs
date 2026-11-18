// EF Core primary-key attribute.
using Microsoft.EntityFrameworkCore;

// Validation and display attributes.
using System.ComponentModel.DataAnnotations;

// Entity namespace.
namespace Ecommerce.Api.Models;

// Primary key is CustomerClassId.
[PrimaryKey(nameof(CustomerClassId))]
// A pricing or service tier assigned to customers, such as retail or wholesale.
public class CustomerClass
{
    // Marks the id property as the key.
    [Key]
    // Validation message when the id is rejected as empty.
    [Required(ErrorMessage = "Customer Class Id cannot be empty.")]
    // UI caption.
    [Display(Name = "Customer Class Id")]
    // Primary key.
    public Guid CustomerClassId { get; set; }

    // The class name must be present.
    [Required(ErrorMessage = "Class Name cannot be empty.")]
    // UI caption "Class Name".
    [Display(Name = "Class Name")]
    // Short name of the tier. Non-nullable, default empty.
    public string ClassName { get; set; } = string.Empty;

    // Description must be present for validation.
    [Required(ErrorMessage = "Description cannot be empty.")]
    // UI caption.
    [Display(Name = "Description")]
    // Longer explanation of the class. Non-nullable, default empty.
    public string Description { get; set; } = string.Empty;

    // UI caption for the creator. The property is optional.
    [Display(Name = "Created By")]
    // Nullable creator. Defaults to empty rather than null.
    public string? CreatedBy { get; set; } = string.Empty;

    // UI caption for the timestamp.
    [Display(Name = "Created On")]
    // Short date, including while the value is shown in an editor.
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
    // Nullable UTC timestamp captured when the object is constructed.
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
}
// Ends the customer-class entity. Customer.CustomerClassId points here.
