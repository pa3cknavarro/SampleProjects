namespace Ecommerce.Api.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(CustomerId))]
public class Customer
{
    [Key] public Guid CustomerId { get; set; }
    [Required] public string CompanyName { get; set; } = string.Empty;
    public Guid CustomerClassId { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public Guid ProvinceId { get; set; }
    public Guid CityId { get; set; }
    public Guid BarangayId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? TinNo { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(CustomerClassId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public CustomerClass CustomerClass { get; set; } = new();
    [ForeignKey(nameof(ProvinceId))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Province Province { get; set; } = new();
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public City City { get; set; } = new();
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public Barangay Barangay { get; set; } = new();
}
