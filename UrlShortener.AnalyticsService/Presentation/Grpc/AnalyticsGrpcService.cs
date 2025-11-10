using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using UrlShortener.Shared.Protos;
using UrlShortener.AnalyticsService.Application.Interfaces;
using UrlShortener.AnalyticsService.Domain.Entities;

namespace UrlShortener.AnalyticsService.Application.Services;

public class AnalyticsGrpcService : AnalyticsGrpc.AnalyticsGrpcBase
{
    private readonly IClickRepository _repo;
    public AnalyticsGrpcService(IClickRepository repo) => _repo = repo;

    public override async Task<Empty> IngestClick(ClickEvent request, ServerCallContext context)
    {
        var record = new ClickRecord
        {
            Id = Guid.NewGuid(),
            ShortUrlId = request.ShortUrlId ?? string.Empty,
            Browser = request.Browser ?? string.Empty,
            Device = request.Device ?? string.Empty,
            Ip = request.Ip ?? string.Empty,
            Referrer = request.Referrer ?? string.Empty,
            UserId = string.IsNullOrEmpty(request.UserId) ? null : request.UserId,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(request.Timestamp)
        };

        await _repo.AddAsync(record);
        return new Empty();
    }

    public override async Task<Empty> IngestLinkCreated(LinkCreatedEvent request, ServerCallContext context)
    {
        var record = new ClickRecord
        {
            Id = Guid.NewGuid(),
            ShortUrlId = request.ShortUrlId ?? string.Empty,
            Browser = "system",
            Device = "system",
            Ip = string.Empty,
            Referrer = request.OriginalUrl ?? string.Empty,
            UserId = string.IsNullOrEmpty(request.UserId) ? null : request.UserId,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(request.Timestamp)
        };

        await _repo.AddAsync(record);
        return new Empty();
    }

    // Helpers
    private void ParsePeriod(string period, out DateTimeOffset from, out DateTimeOffset to)
    {
        to = DateTimeOffset.UtcNow;
        var days = 7;
        if (!string.IsNullOrEmpty(period) && period.EndsWith("d") && int.TryParse(period[..^1], out var p))
            days = p;
        from = to.AddDays(-days);
    }

    public override async Task<PerformanceMetric> GetPerformance(AnalyticsRequest request, ServerCallContext context)
    {
        ParsePeriod(request.Period, out var from, out var to);

        var clicks = await _repo.CountClicksAsync(from, to);
        var records = await _repo.GetByPeriodAsync(from, to);
        var activeUsers = records.Select(r => r.UserId).Where(u => !string.IsNullOrEmpty(u)).Distinct().Count();

        return new PerformanceMetric
        {
            TotalClicks = clicks,
            ActiveUsers = activeUsers
        };
    }

    public override async Task<BrowserDistribution> GetBrowserDistribution(AnalyticsRequest request, ServerCallContext context)
    {
        ParsePeriod(request.Period, out var from, out var to);
        var distribution = await _repo.BrowserDistributionAsync(from, to);

        var resp = new BrowserDistribution();
        foreach (var kv in distribution)
        {
            resp.BrowserNames.Add(kv.Key);
            resp.Counts.Add(kv.Value);
        }

        return resp;
    }

    public override async Task<DeviceDistribution> GetDeviceDistribution(AnalyticsRequest request, ServerCallContext context)
    {
        ParsePeriod(request.Period, out var from, out var to);
        var distribution = await _repo.DeviceDistributionAsync(from, to);

        var resp = new DeviceDistribution();
        foreach (var kv in distribution)
        {
            resp.DeviceTypes.Add(kv.Key);
            resp.Counts.Add(kv.Value);
        }

        return resp;
    }

    public override async Task<TopLinksResponse> GetTopLinks(AnalyticsRequest request, ServerCallContext context)
    {
        ParsePeriod(request.Period, out var from, out var to);
        var limit = request.Limit > 0 ? request.Limit : 10;
        var top = await _repo.GetTopLinksAsync(from, to, limit);

        var resp = new TopLinksResponse();
        // avoid tuple-deconstruction inference issue — iterate and use named properties
        foreach (var item in top)
        {
            resp.Links.Add(new TopLink { ShortUrlId = item.ShortUrlId, Clicks = item.Count });
        }

        return resp;
    }

    public override async Task<UsersResponse> GetUsers(AnalyticsRequest request, ServerCallContext context)
    {
        var users = await _repo.GetUsersAsync();
        var resp = new UsersResponse();
        resp.UserIds.AddRange(users);
        return resp;
    }
}