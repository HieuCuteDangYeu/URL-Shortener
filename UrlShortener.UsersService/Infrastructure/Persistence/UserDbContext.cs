using Microsoft.EntityFrameworkCore;
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
            b.Property(u => u.Role).IsRequired().HasMaxLength(50).HasDefaultValue("User");
            b.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("now()");
            b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
            b.Property(u => u.PasswordSalt).IsRequired().HasMaxLength(128);
        });
    }
}