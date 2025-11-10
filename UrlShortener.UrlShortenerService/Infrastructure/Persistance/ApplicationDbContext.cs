using Microsoft.EntityFrameworkCore;
using UrlShortener.UrlShortenerService.Domain.Entities;

namespace UrlShortener.UrlShortenerService.Infrastructure.Persistance;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ShortLink> ShortLinks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShortLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShortCode).IsUnique();
            entity.Property(e => e.ShortCode).HasMaxLength(10);
            entity.Property(e => e.OriginalUrl).HasMaxLength(2048);
        });
    }
}
