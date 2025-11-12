using Grpc.Core;
using UrlShortener.Shared.Protos;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.Infrastructure.Messaging.Events;
using static UrlShortener.Shared.Protos.UrlShortenerService;
using static UrlShortener.Shared.Protos.UserService;
using UrlShortener.UrlShortenerService.Application.Interfaces;

namespace UrlShortener.UrlShortenerService.Presentation.Grpc;

public class UrlShortenerGrpcService(
    IUrlShortenerService urlShortenerService,
    IMessageBroker messageBroker,
    UserServiceClient userServiceClient,
    IConfiguration configuration) : UrlShortenerServiceBase
{
    public override async Task<CreateShortLinkResponse> CreateShortLink(
        CreateShortLinkRequest request,
        ServerCallContext context)
    {
        try
        {
            Guid? userId = null;
            if (!string.IsNullOrEmpty(request.UserId))
            {
                if (!Guid.TryParse(request.UserId, out var parsedGuid))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid UserId format."));
                }
                userId = parsedGuid;
            }

            var shortLink = await urlShortenerService.CreateShortLinkAsync(request.OriginalUrl);

            var baseUrl = configuration["BaseUrl"] ?? "https://localhost:5001";
            var shortUrl = $"{baseUrl}/Url/{shortLink.ShortCode}";

            // Publish event to message broker
            messageBroker.Publish("url-created", new UrlCreatedEvent
            {
                ShortCode = shortLink.ShortCode,
                OriginalUrl = shortLink.OriginalUrl,
                CreatedAt = shortLink.CreatedAt
            });

            return new CreateShortLinkResponse
            {
                ShortCode = shortLink.ShortCode,
                ShortUrl = shortUrl,
                OriginalUrl = shortLink.OriginalUrl,
                Message = "Short link created successfully"
            };
        }
        catch (FluentValidation.ValidationException ex)
        {
            // Convert FluentValidation errors to gRPC InvalidArgument
            var errors = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GetAllLinksResponse> GetAllLinks(
        GetAllLinksRequest request,
        ServerCallContext context)
    {
        var (links, totalCount) = await urlShortenerService.GetAllLinksAsync(
            request.Page > 0 ? request.Page : 1,
            request.PageSize > 0 ? request.PageSize : 10
        );

        var response = new GetAllLinksResponse
        {
            TotalCount = totalCount
        };

        foreach (var link in links)
        {
            response.Links.Add(new ShortLink
            {
                ShortCode = link.ShortCode,
                OriginalUrl = link.OriginalUrl,
                CreatedAt = link.CreatedAt.ToString("o"),
                ClickCount = link.ClickCount
            });
        }

        return response;
    }

    public override async Task<GetAllLinksResponse> GetLinkByUserId(
        GetAllLinksRequest request,
        ServerCallContext context)
    {
        Guid? userId = null;
        if (!string.IsNullOrEmpty(request.UserId))
        {
            if (!Guid.TryParse(request.UserId, out var parsed))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid UserId format."));
            _ = await userServiceClient.GetUserByIdAsync(new GetUserRequest { Id = request.UserId })
                ?? throw new RpcException(new Status(StatusCode.Unavailable, "User not found."));

            userId = parsed;
        }
        var (links, totalCount) = await urlShortenerService.GetAllLinksAsync(
            request.Page > 0 ? request.Page : 1,
            request.PageSize > 0 ? request.PageSize : 10,
            userId
        );

        var response = new GetAllLinksResponse
        {
            TotalCount = totalCount
        };

        foreach (var link in links)
        {
            response.Links.Add(new ShortLink
            {
                ShortCode = link.ShortCode,
                OriginalUrl = link.OriginalUrl,
                CreatedAt = link.CreatedAt.ToString("o"),
                ClickCount = link.ClickCount
            });
        }

        return response;
    }

    public override async Task<CreateShortLinkResponse> CreateShortLinkByUserId(
        CreateShortLinkRequest request,
        ServerCallContext context)
    {
        try
        {
            Guid? userId = null;
            if (!string.IsNullOrEmpty(request.UserId))
            {
                if (!Guid.TryParse(request.UserId, out var parsed))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid UserId format."));
                _ = await userServiceClient.GetUserByIdAsync(new GetUserRequest { Id = request.UserId })
                    ?? throw new RpcException(new Status(StatusCode.Unavailable, "User not found."));

                userId = parsed;
            }

            var shortLink = await urlShortenerService.CreateShortLinkAsync(request.OriginalUrl, request.CustomCode, userId);

            var baseUrl = configuration["BaseUrl"] ?? "https://localhost:5001";
            var shortUrl = $"{baseUrl}/Url/{shortLink.ShortCode}";

            // Publish event to message broker
            messageBroker.Publish("url-created", new UrlCreatedEvent
            {
                ShortCode = shortLink.ShortCode,
                OriginalUrl = shortLink.OriginalUrl,
                CreatedAt = shortLink.CreatedAt
            });

            return new CreateShortLinkResponse
            {
                ShortCode = shortLink.ShortCode,
                ShortUrl = shortUrl,
                OriginalUrl = shortLink.OriginalUrl,
                Message = "Short link created successfully"
            };
        }
        catch (FluentValidation.ValidationException ex)
        {
            // Convert FluentValidation errors to gRPC InvalidArgument
            var errors = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
}
