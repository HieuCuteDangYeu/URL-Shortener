using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using UrlShortener.UserService.Domain.Entities;

namespace UrlShortener.UserService.Infrastructure.Persistence;

public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);

            b.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            b.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            b.Property(u => u.Email).IsRequired().HasMaxLength(256);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.PhoneNumber).HasMaxLength(32);
            b.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("now()");
            b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
            b.Property(u => u.PasswordSalt).IsRequired().HasMaxLength(128);
        });

        var adminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        const int Pbkdf2Iterations = 100_000;
        const int HashSize = 32;

        var password = "Admin@123";
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var saltBase64 = Convert.ToBase64String(saltBytes);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        var hashBytes = pbkdf2.GetBytes(HashSize);
        var hashBase64 = Convert.ToBase64String(hashBytes);

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminId,
            FirstName = "System",
            LastName = "Administrator",
            Email = "admin@urlshortener.com",
            PhoneNumber = "0000000000",
            PasswordSalt = saltBase64,
            PasswordHash = hashBase64,
            CreatedAt = DateTime.UtcNow
        });
    }
}
