namespace UrlShortener.AnalyticsService.Domain
{
    public class Click
    {
        public Guid Id { get; set; }
        public string ShortCode { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? UserAgent { get; set; }

        public string? Browser { get; set; }
        public string? Referer { get; set; }
        public DateTime OccurredAt { get; set; }

    }
}
