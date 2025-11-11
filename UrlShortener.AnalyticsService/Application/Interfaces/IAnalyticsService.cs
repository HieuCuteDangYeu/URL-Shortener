using UrlShortener.Shared.Protos;

namespace UrlShortener.AnalyticsService.Application.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsOverview> GetOverviewAsync(CancellationToken ct);
    Task<TopLinksResponse> GetTopLinksAsync(int limit, CancellationToken ct);
    Task<BrowserDistributionResponse> GetBrowserDistributionAsync(CancellationToken ct);
    Task<LinkTimelineResponse> GetTimelineAsync(string? shortCode, int days, CancellationToken ct);
}