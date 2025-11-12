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

        var query = db.Clicks.Where(c => c.CreatedAt >= from);
        if (!string.IsNullOrWhiteSpace(shortCode))
            query = query.Where(c => c.ShortCode == shortCode);

        var grouped = await query
            .GroupBy(c => c.CreatedAt.Date)
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

    public static string ParseBrowser(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "Unknown";
        var ua = userAgent.Trim().ToLowerInvariant();

        // Bots / crawlers
        if (ua.Contains("bot") || ua.Contains("spider") || ua.Contains("crawl")) return "Bot";

        // API/CLI clients
        if (ua.Contains("postmanruntime")) return "Postman";
        if (ua.Contains("insomnia")) return "Insomnia";
        if (ua.Contains("curl")) return "curl";

        // Edge (Chromium, iOS, Android)
        if (ua.Contains("edg/") || ua.Contains("edgios") || ua.Contains("edga")) return "Edge";

        // Opera
        if (ua.Contains("opr/") || ua.Contains("opera")) return "Opera";

        // Samsung Internet
        if (ua.Contains("samsungbrowser")) return "Samsung Internet";

        // Vivaldi
        if (ua.Contains("vivaldi")) return "Vivaldi";

        // Yandex
        if (ua.Contains("yabrowser")) return "Yandex";

        // UC Browser
        if (ua.Contains("ucbrowser")) return "UC Browser";

        // Brave
        if (ua.Contains("brave")) return "Brave";

        // Firefox (incl. iOS)
        if (ua.Contains("fxios") || ua.Contains("firefox")) return "Firefox";

        // Chrome on iOS
        if (ua.Contains("crios")) return "Chrome";

        // Explicit Chrome token (covers desktop and Android); exclude Chromium-based forks above
        if (ua.Contains("chrome/") &&
            !ua.Contains("edg/") &&
            !ua.Contains("opr/") &&
            !ua.Contains("samsungbrowser") &&
            !ua.Contains("vivaldi") &&
            !ua.Contains("yabrowser") &&
            !ua.Contains("ucbrowser") &&
            !ua.Contains("brave"))
            return "Chrome";

        // Android WebView heuristics
        if (ua.Contains("; wv") || (ua.Contains("android") && ua.Contains("version/") && ua.Contains("chrome/") && !ua.Contains("samsungbrowser")))
            return "Android WebView";

        // Chromium
        if (ua.Contains("chromium")) return "Chromium";

        // Safari (desktop/mobile), only when Version/ token present and not Chrome/CriOS
        if (ua.Contains("safari") && ua.Contains("version/") && !ua.Contains("chrome") && !ua.Contains("crios"))
            return "Safari";

        // Internet Explorer
        if (ua.Contains("msie") || ua.Contains("trident")) return "IE";

        // Amazon Silk
        if (ua.Contains("silk/")) return "Silk";

        return "Unknown";
    }
}