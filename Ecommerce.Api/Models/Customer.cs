// Entity namespace.
namespace Ecommerce.Api.Models;

// Required attribute on CompanyName.
using System.ComponentModel.DataAnnotations;

// ForeignKey attributes for class and province.
using System.ComponentModel.DataAnnotations.Schema;

// PrimaryKey and DeleteBehavior.
using Microsoft.EntityFrameworkCore;

// Single-column primary key.
[PrimaryKey(nameof(CustomerId))]
// A buying company. Address is stored as province, city, barangay, plus a street line.
public class Customer
{
    // Primary key.
    [Key] public Guid CustomerId { get; set; }

    // Company name is required and non-nullable, defaulting to empty.
    [Required] public string CompanyName { get; set; } = string.Empty;

    // Foreign key to CustomerClass. Every customer has a class id.
    public Guid CustomerClassId { get; set; }

    // Person to reach at the company. Non-nullable, default empty.
    public string ContactPerson { get; set; } = string.Empty;

    // Email stored as plain text. There is no EmailAddress data annotation here.
    public string EmailAddress { get; set; } = string.Empty;

    // Phone number stored as text so formatting characters are kept.
    public string ContactNumber { get; set; } = string.Empty;

    // Province part of the address. Also a foreign key to Province.
    public Guid ProvinceId { get; set; }

    // City part of the address.
    public Guid CityId { get; set; }

    // Barangay part of the address.
    public Guid BarangayId { get; set; }

    // Street or building line. Non-nullable, default empty.
    public string Address { get; set; } = string.Empty;

    // Optional tax identification number. Null when the customer has none.
    public string? TinNo { get; set; }

    // Optional free-text notes. Null when unused.
    public string? Notes { get; set; }

    // Active flag. New customers start active because of the true initializer.
    public bool IsActive { get; set; } = true;

    // Optional creator name.
    public string? CreatedBy { get; set; }

    // Optional creation time, defaulting to UtcNow when the object is new.
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;

    // Class navigation uses CustomerClassId.
    [ForeignKey(nameof(CustomerClassId))]
    // Deleting a class does not cascade to customers.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // Related class. Placeholder until EF loads it.
    public CustomerClass CustomerClass { get; set; } = new();

    // Province navigation uses ProvinceId.
    [ForeignKey(nameof(ProvinceId))]
    // Deleting a province does not cascade to customers.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // Related province. Placeholder until EF loads it.
    public Province Province { get; set; } = new();

    // City is not given an explicit ForeignKey attribute. EF matches it by
    // convention to CityId. Delete is still NoAction.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // Related city. Placeholder until EF loads it.
    public City City { get; set; } = new();

    // Barangay is also matched by convention to BarangayId. Delete is NoAction.
    [DeleteBehavior(DeleteBehavior.NoAction)]
    // Related barangay. Placeholder until EF loads it.
    public Barangay Barangay { get; set; } = new();
}
// Ends the customer entity.
