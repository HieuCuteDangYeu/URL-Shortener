using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Auth.Domain.Entities;

public class UserRole
{
    [Key]
    public Guid Id { get; set; }
    [Required] public Guid UserId { get; set; }
    [Required] public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
