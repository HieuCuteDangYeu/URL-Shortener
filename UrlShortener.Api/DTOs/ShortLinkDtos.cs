namespace UrlShortener.Api.DTOs
{
    public class CreateShortLinkDto
    {
        public string OriginalUrl { get; set; } = null!;
    }

    public class ShortLinkResponseDto
    {
        public string OriginalUrl { get; set; } = null!;
        public string ShortUrl { get; set; } = null!;
    }
}
