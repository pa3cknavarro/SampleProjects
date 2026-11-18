// Entity namespace.
namespace Ecommerce.Api.Models;

// Required, Display, and DisplayFormat attributes.
using System.ComponentModel.DataAnnotations;

// Roles enum used by the Role property.
using Ecommerce.Api.Models.Enumerations;

// PrimaryKey attribute.
using Microsoft.EntityFrameworkCore;

// Surrogate primary key named Id.
[PrimaryKey(nameof(Id))]
// An account that can own a cart and authenticate to the API.
public class User
{
    // Marks Id as the key.
    [Key]
    // UI caption. There is no Required attribute on the id.
    [Display(Name = "User Id")]
    // Primary key.
    public Guid Id { get; set; }

    // Company must be present for validation.
    [Required(ErrorMessage = "Company is required.")]
    // UI caption says "Company Id" even though the error text says "Company".
    [Display(Name = "Company Id")]
    // Company the user belongs to. Stored as a Guid with no navigation property on this class.
    public Guid CompanyId { get; set; }

    // Username is required.
    [Required(ErrorMessage = "Username is required.")]
    // UI caption.
    [Display(Name = "Username")]
    // Login name. Non-nullable, default empty.
    public string Username { get; set; } = string.Empty;

    // Email is used for registration, verification, and password recovery.
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // A one-way password hash; plaintext credentials are never persisted.
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // Optional customer account linked to this user.
    public Guid? CustomerId { get; set; }

    public bool MfaEnabled { get; set; }

    public string? MfaSecretProtected { get; set; }

    // A role value is required.
    [Required(ErrorMessage = "Role is required.")]
    // UI caption.
    [Display(Name = "Role")]
    // Enum stored as its underlying int. New users start as Roles.User, which is 0.
    public Roles Role { get; set; } = Roles.User;

    // UI caption phrased as a question for a checkbox label.
    [Display(Name = "Is Active?")]
    // Active flag. New users start active.
    public bool IsActive { get; set; } = true;

    // UI caption for the creator.
    [Display(Name = "Created By")]
    // Audit user. Non-nullable, default empty.
    public string CreatedBy { get; set; } = string.Empty;

    // UI caption for the timestamp.
    [Display(Name = "Created On")]
    // Short-date display format.
    [DisplayFormat(DataFormatString = "{0:d}")]
    // UTC timestamp captured when the user object is constructed.
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
// Ends the user entity.
