using System.ComponentModel.DataAnnotations;

namespace UrlShortener.AnalyticsService.Domain.Entities;

public class ClickRecord
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(64)]
    public string ShortUrlId { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    [StringLength(128)]
    public string Browser { get; set; } = string.Empty;

    [StringLength(128)]
    public string Device { get; set; } = string.Empty;

    [StringLength(64)]
    public string Ip { get; set; } = string.Empty;

    [StringLength(2048)]
    public string Referrer { get; set; } = string.Empty;

    [StringLength(128)]
    public string? UserId { get; set; }
}