namespace UrlShortener.Infrastructure.Messaging.Events;

public class UrlClickedEvent
{
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? UserId { get; set; }
    public string? UserAgent { get; set; }
    public string? Referrer { get; set; }
}
