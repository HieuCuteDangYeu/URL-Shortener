using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using UrlShortener.Auth.Application.Interfaces;
using UrlShortener.Shared.Protos;
using static UrlShortener.Shared.Protos.AuthService;

namespace UrlShortener.Auth.Presentation.Grpc;

public class AuthGrpcService(IAuthService authService) : AuthServiceBase
{
    public override async Task<AuthResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        try
        {
            var response = await authService.RegisterAsync(request);
            return response;
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred during registration"));
        }
    }

    public override async Task<AuthResponse> Login(LoginRequest request, ServerCallContext context)
    {
        try
        {
            var response = await authService.LoginAsync(request);
            return response;
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, ex.Message));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred during login"));
        }
    }

    public override async Task<AuthResponse> RefreshToken(RefreshTokenRequest request, ServerCallContext context)
    {
        try
        {
            var response = await authService.RefreshTokenAsync(request);
            return response;
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, ex.Message));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred during token refresh"));
        }
    }

    public override async Task<Empty> RevokeToken(RevokeTokenRequest request, ServerCallContext context)
    {
        try
        {
            await authService.RevokeTokenAsync(request);
            return new Empty();
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred during token revocation"));
        }
    }
}
