namespace UrlShortener.Infrastructure.Messaging.Events
{
    public class ShortLinkClickedEvent
    {
        public string ShortCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? UserId { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        public string? Referer { get; set; }
        public string? Browser { get; set; }
    }
}
