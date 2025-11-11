using Microsoft.EntityFrameworkCore;
using UrlShortener.AnalyticsService.Application.Interfaces;
using UrlShortener.AnalyticsService.Infrastructure.Persistence;
using UrlShortener.Shared.Protos;

namespace UrlShortener.AnalyticsService.Application.Services;

public class AnalyticsService(AnalyticsDbContext db) : IAnalyticsService
{
    public async Task<AnalyticsOverview> GetOverviewAsync(CancellationToken ct)
    {
        var totalClicks = await db.Clicks.LongCountAsync(ct);
        var totalLinks = await db.Clicks.Select(c => c.ShortCode).Distinct().CountAsync(ct);
        var totalUsers = await db.Clicks.Where(c => c.UserId != null && c.UserId != "")
                                        .Select(c => c.UserId!).Distinct().CountAsync(ct);

        // Browser stats
        var browserGroups = await db.Clicks
            .GroupBy(c => c.Browser ?? "Unknown")
            .Select(g => new { Browser = g.Key, Count = g.LongCount() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var overview = new AnalyticsOverview
        {
            TotalClicks = totalClicks,
            TotalLinks = totalLinks,
            TotalUsers = totalUsers
        };

        foreach (var b in browserGroups)
        {
            overview.Browsers.Add(new BrowserStat
            {
                Browser = b.Browser,
                Clicks = b.Count,
                Percentage = totalClicks == 0 ? 0 : (double)b.Count / totalClicks * 100d
            });
        }

        // Top 5 links inline
        var topLinks = await db.Clicks
            .GroupBy(c => c.ShortCode)
            .Select(g => new { ShortCode = g.Key, Count = g.LongCount() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(ct);

        foreach (var l in topLinks)
        {
            overview.TopLinks.Add(new TopLink { ShortCode = l.ShortCode, Clicks = l.Count });
        }

        return overview;
    }

    public async Task<TopLinksResponse> GetTopLinksAsync(int limit, CancellationToken ct)
    {
        limit = limit <= 0 ? 10 : limit;
        var data = await db.Clicks
            .GroupBy(c => c.ShortCode)
            .Select(g => new { ShortCode = g.Key, Count = g.LongCount() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync(ct);

        var resp = new TopLinksResponse();
        foreach (var row in data)
            resp.Links.Add(new TopLink { ShortCode = row.ShortCode, Clicks = row.Count });

        return resp;
    }

    public async Task<BrowserDistributionResponse> GetBrowserDistributionAsync(CancellationToken ct)
    {
        var total = await db.Clicks.LongCountAsync(ct);
        var groups = await db.Clicks
            .GroupBy(c => c.Browser ?? "Unknown")
            .Select(g => new { Browser = g.Key, Count = g.LongCount() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var resp = new BrowserDistributionResponse();
        foreach (var g in groups)
        {
            resp.Browsers.Add(new BrowserStat
            {
                Browser = g.Browser,
                Clicks = g.Count,
                Percentage = total == 0 ? 0 : (double)g.Count / total * 100d
            });
        }
        return resp;
    }

    public async Task<LinkTimelineResponse> GetTimelineAsync(string? shortCode, int days, CancellationToken ct)
    {
        days = days <= 0 ? 7 : days;
        var from = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var query = db.Clicks.Where(c => c.OccurredAt >= from);
        if (!string.IsNullOrWhiteSpace(shortCode))
            query = query.Where(c => c.ShortCode == shortCode);

        var grouped = await query
            .GroupBy(c => c.OccurredAt.Date)
            .Select(g => new { Date = g.Key, Count = g.LongCount() })
            .ToListAsync(ct);

        var resp = new LinkTimelineResponse { ShortCode = shortCode ?? "" };

        for (int i = 0; i < days; i++)
        {
            var day = from.AddDays(i);
            var match = grouped.FirstOrDefault(g => g.Date == day);
            resp.Points.Add(new TimelinePoint
            {
                Date = day.ToString("yyyy-MM-dd"),
                Clicks = match?.Count ?? 0
            });
        }

        return resp;
    }
}