using UrlShortener.UrlShortenerService.Domain.Entities;

namespace UrlShortener.UrlShortenerService.Application.Interfaces;

public interface IUrlShortenerService
{
    Task<ShortLink> CreateShortLinkAsync(string originalUrl, string? customCode = null, Guid? userId = null);
    Task<ShortLink?> GetOriginalUrlAsync(string shortCode);
    Task<(List<ShortLink> Links, int TotalCount)> GetAllLinksAsync(int page, int pageSize, Guid? userId = null);
    Task IncrementClickCountAsync(string shortCode);
}
