using PharmacyManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PharmacyManagement.Domain.Models;

public class User : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Pharmacist;

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginDate { get; set; }
}
