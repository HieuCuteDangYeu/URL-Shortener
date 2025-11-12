using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Auth.Domain.Entities;

public class Role
{
    [Key]
    public Guid Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;
}
