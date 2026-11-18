namespace Ecommerce.Api.Models;

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(RecoveryCodeId))]
public sealed class MfaRecoveryCode
{
    [Key]
    public Guid RecoveryCodeId { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(64)]
    public string CodeHash { get; set; } = string.Empty;
}
