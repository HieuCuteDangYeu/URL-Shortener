using Microsoft.EntityFrameworkCore;
using UrlShortener.Auth.Domain.Entities;

namespace UrlShortener.Auth.Infrastructure.Persistence;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);

            entity.Property(rt => rt.UserId).IsRequired();
            entity.Property(rt => rt.Token).IsRequired().HasMaxLength(512);
            entity.Property(rt => rt.ExpiresAt).IsRequired();
            entity.Property(rt => rt.CreatedAt).IsRequired().HasDefaultValueSql("now()");
            entity.Property(rt => rt.RevokedAt).IsRequired(false);
            entity.Property(rt => rt.ReplacedByToken).HasMaxLength(512).IsRequired(false);

            // Index for faster lookups
            entity.HasIndex(rt => rt.Token);
            entity.HasIndex(rt => rt.UserId);
        });
    }
}
