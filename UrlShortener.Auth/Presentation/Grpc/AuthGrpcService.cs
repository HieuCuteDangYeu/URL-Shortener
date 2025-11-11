using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using UrlShortener.Auth.Application.Interfaces;
using UrlShortener.Auth.Protos;
using static UrlShortener.Auth.Protos.AuthService;

namespace UrlShortener.Auth.Presentation.Grpc;

public class AuthGrpcService : AuthServiceBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthGrpcService> _logger;

    public AuthGrpcService(IAuthService authService, ILogger<AuthGrpcService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public override async Task<AuthResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Register request for email: {Email}", request.Email);
            var response = await _authService.RegisterAsync(request);
            return response;
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            _logger.LogWarning("Validation failed for register: {Errors}", errors);
            throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred during registration"));
        }
    }

    public override async Task<AuthResponse> Login(LoginRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Login request for email: {Email}", request.Email);
            var response = await _authService.LoginAsync(request);
            return response;
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            _logger.LogWarning("Validation failed for login: {Errors}", errors);
            throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.Unauthenticated, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred during login"));
        }
    }

    public override async Task<AuthResponse> RefreshToken(RefreshTokenRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Refresh token request");
            var response = await _authService.RefreshTokenAsync(request);
            return response;
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            _logger.LogWarning("Validation failed for refresh token: {Errors}", errors);
            throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Refresh token failed: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.Unauthenticated, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred during token refresh"));
        }
    }

    public override async Task<Empty> RevokeToken(RevokeTokenRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Revoke token request");
            await _authService.RevokeTokenAsync(request);
            return new Empty();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Token revocation failed: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred during token revocation"));
        }
    }
}
