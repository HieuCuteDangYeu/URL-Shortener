using Microsoft.EntityFrameworkCore;
using UrlShortener.AnalyticsService.Application.Interfaces;
using UrlShortener.AnalyticsService.Domain.Entities;
using UrlShortener.AnalyticsService.Infrastructure.Persistence;

namespace UrlShortener.AnalyticsService.Application.Services;

public class ClickRepository : IClickRepository
{
    private readonly AnalyticsDbContext _db;
    public ClickRepository(AnalyticsDbContext db) => _db = db;

    public async Task AddAsync(ClickRecord record)
    {
        await _db.ClickRecords.AddAsync(record);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ClickRecord>> GetByPeriodAsync(DateTimeOffset from, DateTimeOffset to)
    {
        return await _db.ClickRecords
            .Where(c => c.Timestamp >= from && c.Timestamp <= to)
            .ToListAsync();
    }

    public async Task<int> CountClicksAsync(DateTimeOffset from, DateTimeOffset to)
    {
        return await _db.ClickRecords
            .CountAsync(c => c.Timestamp >= from && c.Timestamp <= to);
    }

    public async Task<Dictionary<string, int>> BrowserDistributionAsync(DateTimeOffset from, DateTimeOffset to)
    {
        return await _db.ClickRecords
            .Where(c => c.Timestamp >= from && c.Timestamp <= to)
            .GroupBy(c => c.Browser)
            .Select(g => new { Browser = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Browser ?? "Unknown", x => x.Count);
    }

    public async Task<Dictionary<string, int>> DeviceDistributionAsync(DateTimeOffset from, DateTimeOffset to)
    {
        return await _db.ClickRecords
            .Where(c => c.Timestamp >= from && c.Timestamp <= to)
            .GroupBy(c => c.Device)
            .Select(g => new { Device = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Device ?? "Unknown", x => x.Count);
    }

    public async Task<List<(string ShortUrlId, int Count)>> GetTopLinksAsync(DateTimeOffset from, DateTimeOffset to, int limit)
    {
        var list = await _db.ClickRecords
            .Where(c => c.Timestamp >= from && c.Timestamp <= to)
            .GroupBy(c => c.ShortUrlId)
            .Select(g => new { ShortUrlId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync();

        return list.Select(x => (x.ShortUrlId ?? string.Empty, x.Count)).ToList();
    }

    public async Task<IEnumerable<string>> GetUsersAsync()
    {
        return await _db.ClickRecords
            .Where(c => !string.IsNullOrEmpty(c.UserId))
            .Select(c => c.UserId!)
            .Distinct()
            .ToListAsync();
    }
}