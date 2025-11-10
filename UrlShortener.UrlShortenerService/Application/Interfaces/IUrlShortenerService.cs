using UrlShortener.UrlShortenerService.Domain.Entities;

namespace UrlShortener.UrlShortenerService.Application.Interfaces;

public interface IUrlShortenerService
{
    Task<ShortLink> CreateShortLinkAsync(string originalUrl, string? customCode = null, int? userId = null);
    Task<ShortLink?> GetOriginalUrlAsync(string shortCode);
    Task<(List<ShortLink> Links, int TotalCount)> GetAllLinksAsync(int page, int pageSize, int? userId = null);
    Task IncrementClickCountAsync(string shortCode);
}
