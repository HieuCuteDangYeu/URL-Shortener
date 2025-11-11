using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using UrlShortener.Shared.Protos;
using UrlShortener.AnalyticsService.Application.Interfaces;
using static UrlShortener.Shared.Protos.AnalyticsService;

namespace UrlShortener.AnalyticsService.Presentation.Grpc;

public class AnalyticsGrpcService(IAnalyticsService service) : AnalyticsServiceBase
{
    public override Task<AnalyticsOverview> GetOverview(Empty request, ServerCallContext context) =>
        service.GetOverviewAsync(context.CancellationToken);

    public override Task<TopLinksResponse> GetTopLinks(TopLinksRequest request, ServerCallContext context) =>
        service.GetTopLinksAsync(request.Limit, context.CancellationToken);

    public override Task<BrowserDistributionResponse> GetBrowserDistribution(Empty request, ServerCallContext context) =>
        service.GetBrowserDistributionAsync(context.CancellationToken);

    public override Task<LinkTimelineResponse> GetLinkTimeline(LinkTimelineRequest request, ServerCallContext context) =>
        service.GetTimelineAsync(string.IsNullOrWhiteSpace(request.ShortCode) ? null : request.ShortCode,
            request.Days, context.CancellationToken);
}