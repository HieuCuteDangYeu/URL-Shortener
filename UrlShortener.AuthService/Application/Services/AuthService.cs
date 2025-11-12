using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using UrlShortener.Auth.Application.Interfaces;
using UrlShortener.Auth.Domain.Entities;
using UrlShortener.Auth.Infrastructure.Persistence;
using UrlShortener.Auth.Infrastructure.Security;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.Infrastructure.Messaging.Events;
using UrlShortener.Shared.Protos;
using static UrlShortener.Shared.Protos.UserService;

namespace UrlShortener.Auth.Application.Services;

public class AuthService(
    UserServiceClient userServiceClient,
    IJwtTokenGenerator jwtTokenGenerator,
    AuthDbContext dbContext,
    IMessageBroker messageBroker,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshTokenRequest> refreshTokenValidator) : IAuthService
{
    private const int RefreshTokenExpiryDays = 30;
    private const int Pbkdf2Iterations = 100_000;
    private const int HashSize = 32;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var validationResult = await registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var createUserRequest = new CreateUserRequest
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber ?? string.Empty,
            Password = request.Password
        };

        var user = await userServiceClient.CreateUserAsync(createUserRequest);

        var userId = Guid.Parse(user.Id);
        await AssignDefaultRoleAsync(userId);

        var roles = await GetUserRolesAsync(userId);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(userId, user.Email, roles);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

        await StoreRefreshTokenAsync(userId, refreshToken);

        messageBroker.Publish("user-registered", new UserCreatedEvent
        {
            Id = userId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
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
            Roles = { roles },
            ExpiresIn = 1800
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var validationResult = await loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var user = await userServiceClient.GetUserByEmailAsync(new GetUserByEmailRequest
        {
            Email = request.Email
        });

        if (!VerifyPassword(request.Password, user.PasswordSalt, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        var userId = Guid.Parse(user.Id);
        var roles = await GetUserRolesAsync(userId);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(userId, user.Email, roles);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

        await StoreRefreshTokenAsync(userId, refreshToken);

        messageBroker.Publish("user-logged-in", new
        {
            UserId = userId,
            user.Email,
            LoginAt = DateTime.UtcNow
        });

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = { roles },
            ExpiresIn = 1800
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow && rt.Token == request.RefreshToken) ?? throw new UnauthorizedAccessException("Invalid or expired refresh token");
        var userId = storedToken.UserId;

        var response = await userServiceClient.GetAllUsersAsync(new GetAllUsersRequest
        {
            Id = userId.ToString(),
            Page = 1,
            PageSize = 1
        });

        var user = response.Users.FirstOrDefault()
            ?? throw new UnauthorizedAccessException("User not found");

        var roles = await GetUserRolesAsync(userId);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(userId, user.Email, roles);
        var newRefreshToken = jwtTokenGenerator.GenerateRefreshToken();

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken;
        dbContext.RefreshTokens.Update(storedToken);

        await StoreRefreshTokenAsync(userId, newRefreshToken);

        messageBroker.Publish("token-refreshed", new
        {
            userId,
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
            Roles = { roles },
            ExpiresIn = 1800
        };
    }

    public async Task RevokeTokenAsync(RevokeTokenRequest request)
    {
        var token = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken)
            ?? throw new InvalidOperationException("Token not found");

        if (token.IsRevoked)
            throw new InvalidOperationException("Token already revoked");

        token.RevokedAt = DateTime.UtcNow;
        dbContext.RefreshTokens.Update(token);
        await dbContext.SaveChangesAsync();

        messageBroker.Publish("token-revoked", new
        {
            token.UserId,
            RevokedAt = DateTime.UtcNow
        });
    }

    private async Task StoreRefreshTokenAsync(Guid userId, string token)
    {
        // Find existing active token directly in SQL
        var existingToken = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (existingToken != null)
        {
            // Revoke old token
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.ReplacedByToken = token;
            dbContext.RefreshTokens.Update(existingToken);
        }

        // Add new token
        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await dbContext.RefreshTokens.AddAsync(newToken);
        await dbContext.SaveChangesAsync();
    }

    private static bool VerifyPassword(string password, string salt, string expectedHash)
    {
        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);
        var computed = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(computed),
            Convert.FromBase64String(expectedHash)
        );
    }

    private async Task AssignDefaultRoleAsync(Guid userId)
    {
        var defaultRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "User")
            ?? throw new InvalidOperationException("Default role 'User' not found");

        await dbContext.UserRoles.AddAsync(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = defaultRole.Id
        });

        await dbContext.SaveChangesAsync();
    }

    private async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        return await dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }
}
