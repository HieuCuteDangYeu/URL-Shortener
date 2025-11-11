using System.ComponentModel.DataAnnotations;

namespace UrlShortener.UserService.Domain.Entities;

public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(32)]
    public string? PhoneNumber { get; set; }

    [Required, StringLength(50)]
    public string Role { get; set; } = "User";

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Password storage (never returned to clients)
    [Required, StringLength(512)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string PasswordSalt { get; set; } = string.Empty;
}