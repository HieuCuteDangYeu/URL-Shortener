using UrlShortener.AnalyticsService.Domain.Entities;

namespace UrlShortener.AnalyticsService.Application.Interfaces;

public interface IClickRepository
{
    Task AddAsync(ClickRecord record);
    Task<IEnumerable<ClickRecord>> GetByPeriodAsync(DateTimeOffset from, DateTimeOffset to);
    Task<int> CountClicksAsync(DateTimeOffset from, DateTimeOffset to);
    Task<Dictionary<string, int>> BrowserDistributionAsync(DateTimeOffset from, DateTimeOffset to);
    Task<Dictionary<string, int>> DeviceDistributionAsync(DateTimeOffset from, DateTimeOffset to);
    Task<List<(string ShortUrlId, int Count)>> GetTopLinksAsync(DateTimeOffset from, DateTimeOffset to, int limit);
    Task<IEnumerable<string>> GetUsersAsync();
}