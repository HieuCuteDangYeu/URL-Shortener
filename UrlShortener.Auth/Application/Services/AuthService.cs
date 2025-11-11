using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Auth.Application.Interfaces;
using UrlShortener.Auth.Domain.Entities;
using UrlShortener.Auth.Infrastructure.Persistence;
using UrlShortener.Auth.Infrastructure.Security;
using UrlShortener.Auth.Protos;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.Infrastructure.Messaging.Events;
using UrlShortener.Shared.Protos;
using System.Security.Cryptography;

namespace UrlShortener.Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserService.UserServiceClient _userServiceClient;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly AuthDbContext _dbContext;
    private readonly IMessageBroker _messageBroker;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private const int RefreshTokenExpiryDays = 30;
    private const int Pbkdf2Iterations = 100_000;
    private const int HashSize = 32;

    public AuthService(
        UserService.UserServiceClient userServiceClient,
        IJwtTokenGenerator jwtTokenGenerator,
        AuthDbContext dbContext,
        IMessageBroker messageBroker,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator)
    {
        _userServiceClient = userServiceClient;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dbContext = dbContext;
        _messageBroker = messageBroker;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Validate request
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        try
        {
            // Call UserService to create user
            var createUserRequest = new CreateUserRequest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber ?? string.Empty,
                Password = request.Password,
                Role = "User"
            };

            var user = await _userServiceClient.CreateUserAsync(createUserRequest);

            // Generate tokens
            var userId = Guid.Parse(user.Id);
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(userId, user.Email, user.Role);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            // Store refresh token
            await StoreRefreshTokenAsync(userId, refreshToken);

            // Publish event
            _messageBroker.Publish("user-registered", new UserCreatedEvent
            {
                Id = userId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = DateTime.UtcNow
            });

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                ExpiresIn = 1800 // 30 minutes
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            throw new InvalidOperationException("Email already in use");
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Validate request
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        try
        {
            // Get user from UserService with credentials
            var getUserRequest = new GetUserByEmailRequest { Email = request.Email };
            var userWithCreds = await _userServiceClient.GetUserByEmailAsync(getUserRequest);

            // Verify password
            if (!VerifyPassword(request.Password, userWithCreds.PasswordSalt, userWithCreds.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password");

            // Generate tokens
            var userId = Guid.Parse(userWithCreds.Id);
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(userId, userWithCreds.Email, userWithCreds.Role);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            // Store refresh token
            await StoreRefreshTokenAsync(userId, refreshToken);

            // Publish event
            _messageBroker.Publish("user-logged-in", new
            {
                UserId = userId,
                Email = userWithCreds.Email,
                LoginAt = DateTime.UtcNow
            });

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = userWithCreds.Id,
                Email = userWithCreds.Email,
                FirstName = userWithCreds.FirstName,
                LastName = userWithCreds.LastName,
                Role = userWithCreds.Role,
                ExpiresIn = 1800 // 30 minutes
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // Validate request
        var validationResult = await _refreshTokenValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // Find refresh token in database
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken == null || !storedToken.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        // Get user from UserService
        var getUserRequest = new GetUserRequest { Id = storedToken.UserId.ToString() };
        User user;
        try
        {
            var response = await _userServiceClient.GetAllUsersAsync(new GetAllUsersRequest
            {
                Id = storedToken.UserId.ToString(),
                Page = 1,
                PageSize = 1
            });
            user = response.Users.FirstOrDefault()
                ?? throw new UnauthorizedAccessException("User not found");
        }
        catch (RpcException)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        // Generate new tokens
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(storedToken.UserId, user.Email, user.Role);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        // Revoke old token and store new one
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken;
        await _dbContext.SaveChangesAsync();

        await StoreRefreshTokenAsync(storedToken.UserId, newRefreshToken);

        // Publish event
        _messageBroker.Publish("token-refreshed", new
        {
            UserId = storedToken.UserId,
            RefreshedAt = DateTime.UtcNow
        });

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            ExpiresIn = 1800 // 30 minutes
        };
    }

    public async Task RevokeTokenAsync(RevokeTokenRequest request)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (token == null)
            throw new InvalidOperationException("Token not found");

        if (token.IsRevoked)
            throw new InvalidOperationException("Token already revoked");

        token.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        // Publish event
        _messageBroker.Publish("token-revoked", new
        {
            UserId = token.UserId,
            RevokedAt = DateTime.UtcNow
        });
    }

    private async Task StoreRefreshTokenAsync(Guid userId, string token)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();
    }

    private static bool VerifyPassword(string password, string salt, string expectedHash)
    {
        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);
        var computed = Convert.ToBase64String(hash);

        var a = Convert.FromBase64String(computed);
        var b = Convert.FromBase64String(expectedHash);
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
