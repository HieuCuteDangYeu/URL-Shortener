using Microsoft.EntityFrameworkCore;
using UrlShortener.Auth.Domain.Entities;

namespace UrlShortener.Auth.Infrastructure.Persistence
{
    public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
    {
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                entity.Property(rt => rt.UserId).IsRequired();
                entity.Property(rt => rt.Token).IsRequired().HasMaxLength(512);
                entity.Property(rt => rt.ExpiresAt).IsRequired();
                entity.Property(rt => rt.CreatedAt).IsRequired().HasDefaultValueSql("now()");
                entity.Property(rt => rt.RevokedAt).IsRequired(false);
                entity.Property(rt => rt.ReplacedByToken).HasMaxLength(512).IsRequired(false);
                entity.HasIndex(rt => rt.Token);
                entity.HasIndex(rt => rt.UserId);
            });

            // Role configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(r => r.Name).IsUnique();
                entity.HasData(
                    new Role
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Name = "Admin"
                    },
                    new Role
                    {
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        Name = "User"
                    }
                );
            });

            // UserRole configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => ur.Id);
                entity.Property(ur => ur.UserId).IsRequired();
                entity.Property(ur => ur.RoleId).IsRequired();
                entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
            });

            var adminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            modelBuilder.Entity<UserRole>().HasData(
                new UserRole
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    UserId = adminUserId,
                    RoleId = adminRoleId
                }
            );
        }
    }
}
