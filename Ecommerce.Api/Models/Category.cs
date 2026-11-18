// EF Core attributes, including PrimaryKey, are in this namespace.
using Microsoft.EntityFrameworkCore;

// Validation and display attributes applied to the category properties.
using System.ComponentModel.DataAnnotations;

// Entity namespace. Usings above the namespace are file-scoped imports for this file only.
namespace Ecommerce.Api.Models;

// Single-column primary key named CategoryId.
[PrimaryKey(nameof(CategoryId))]
// A product grouping such as "Beverages". Mapped to its own table by convention.
public class Category
{
    // Identifies this property as the key.
    [Key]
    // Validation error text when the id fails the required check.
    [Required(ErrorMessage = "Category Id cannot be empty.")]
    // UI caption for the id.
    [Display(Name = "Category Id")]
    // Primary key value. Guid.Empty until the application assigns one.
    public Guid CategoryId { get; set; }

    // Name must be present for validation.
    [Required(ErrorMessage = "Category Name cannot be empty.")]
    // UI caption "Category Name".
    [Display(Name = "Category Name")]
    // Nullable string annotation even though it is initialized to "". The ? allows
    // later code to assign null, and EF will map a nullable column.
    public string? CategoryName { get; set; } = string.Empty;

    // UI caption for the creator.
    [Display(Name = "Created By")]
    // Optional creator name. Nullable, with an empty-string default.
    public string? CreatedBy { get; set; } = string.Empty;

    // UI caption for the timestamp.
    [Display(Name = "Created On")]
    // ApplyFormatInEditMode true means the format is also used in editor templates.
    // {0:d} is the short-date pattern.
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
    // Nullable UTC timestamp captured when the object is constructed.
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
}
// Ends the Category entity. Products point at this row through CategoryId.
