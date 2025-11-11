using FluentValidation;
using System.Security.Cryptography;
using UrlShortener.UserService.Application.Interfaces;
using UrlShortener.UserService.Application.Requests;
using UrlShortener.UserService.Application.Responses;
using UrlShortener.UserService.Domain.Entities;
using UrlShortener.UserService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace UrlShortener.UserService.Application.Services;

public class UserService(UserDbContext db, IValidator<CreateUserRequest> createValidator, IValidator<UpdateUserRequest> updateValidator) : IUserService
{
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var result = await createValidator.ValidateAsync(request);
        if (!result.IsValid) throw new ValidationException(result.Errors);

        var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());
        if (exists) throw new InvalidOperationException("Email already in use");

        var salt = GenerateSalt();
        var hash = HashPassword(request.Password, salt);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role,
            CreatedAt = DateTime.UtcNow,
            PasswordSalt = salt,
            PasswordHash = hash
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        return Map(user);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        return user == null ? null : Map(user);
    }

    public async Task<(List<UserDto> Users, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Users.AsQueryable();
        var total = await query.CountAsync();

        var list = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (list.Select(Map).ToList(), total);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var validation = await updateValidator.ValidateAsync(request);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return null;

        if (!string.IsNullOrWhiteSpace(request.FirstName)) user.FirstName = request.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(request.LastName)) user.LastName = request.LastName.Trim();
        if (request.PhoneNumber != null) user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        if (!string.IsNullOrWhiteSpace(request.Role)) user.Role = request.Role;

        if (!string.IsNullOrEmpty(request.Password))
        {
            var newSalt = GenerateSalt();
            var newHash = HashPassword(request.Password, newSalt);
            user.PasswordSalt = newSalt;
            user.PasswordHash = newHash;
        }

        db.Users.Update(user);
        await db.SaveChangesAsync();

        return Map(user);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> AuthenticateAsync(string email, string password)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        if (user == null) return null;
        if (!VerifyPassword(password, user.PasswordSalt, user.PasswordHash)) return null;
        return Map(user);
    }

    // Hash helpers
    private static string GenerateSalt()
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        return Convert.ToBase64String(salt);
    }

    private static string HashPassword(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);
        return Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string salt, string expectedHash)
    {
        var computed = HashPassword(password, salt);
        var a = Convert.FromBase64String(computed);
        var b = Convert.FromBase64String(expectedHash);
        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static UserDto Map(User u) => new()
    {
        Id = u.Id,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        Role = u.Role,
        CreatedAt = u.CreatedAt
    };
}