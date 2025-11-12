using FluentValidation;
using System.Security.Cryptography;
using UrlShortener.UserService.Application.Interfaces;
using UrlShortener.UserService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Shared.Protos;

namespace UrlShortener.UserService.Application.Services;

public class UserService(UserDbContext db, IValidator<CreateUserRequest> createValidator, IValidator<UpdateUserRequest> updateValidator) : IUserService
{
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public async Task<Domain.Entities.User> CreateAsync(CreateUserRequest request)
    {
        var result = await createValidator.ValidateAsync(request);
        if (!result.IsValid) throw new ValidationException(result.Errors);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var exists = await db.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (exists) throw new InvalidOperationException("Email already in use");

        var salt = GenerateSalt();
        var hash = HashPassword(request.Password, salt);

        var user = new Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            PasswordSalt = salt,
            PasswordHash = hash
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        return user;
    }

    public async Task<Domain.Entities.User?> GetByIdAsync(Guid id)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Domain.Entities.User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    }

    public async Task<(List<Domain.Entities.User> Users, int TotalCount)> GetAllAsync(Guid? id, int page, int pageSize)
    {
        var query = db.Users.AsQueryable();

        if (id.HasValue)
        {
            query = query.Where(u => u.Id == id.Value);
        }

        var totalCount = await query.CountAsync();

        var links = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (links, totalCount);
    }

    public async Task<Domain.Entities.User?> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var validation = await updateValidator.ValidateAsync(request);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return null;

        if (request.FirstName != null)
            user.FirstName = request.FirstName.Trim();

        if (request.LastName != null)
            user.LastName = request.LastName.Trim();

        if (request.PhoneNumber != null)
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        if (request.Email != null)
            user.Email = request.Email.Trim().ToLowerInvariant();

        if (request.Password != null)
        {
            var newSalt = GenerateSalt();
            var newHash = HashPassword(request.Password, newSalt);
            user.PasswordSalt = newSalt;
            user.PasswordHash = newHash;
        }

        db.Users.Update(user);
        await db.SaveChangesAsync();

        return user;
    }


    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return true;
    }

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
}