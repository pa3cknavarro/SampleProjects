namespace Ecommerce.Api.Models;

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(NotificationId))]
public sealed class Notification
{
    [Key]
    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? ReadOn { get; set; }

    public User User { get; set; } = null!;
}
