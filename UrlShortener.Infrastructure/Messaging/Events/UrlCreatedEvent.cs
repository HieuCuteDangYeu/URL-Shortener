namespace UrlShortener.Infrastructure.Messaging.Events;

public class UrlCreatedEvent
{
    public string ShortCode { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
