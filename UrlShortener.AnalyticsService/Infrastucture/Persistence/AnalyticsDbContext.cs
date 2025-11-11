using Microsoft.EntityFrameworkCore;
using UrlShortener.AnalyticsService.Domain;

namespace UrlShortener.AnalyticsService.Infrastructure.Persistence;

public class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    public DbSet<Click> Clicks => Set<Click>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Click>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ShortCode).HasMaxLength(32).IsRequired();
            e.Property(x => x.Browser).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(512);
            e.HasIndex(x => x.ShortCode);
            e.HasIndex(x => x.OccurredAt);
        });
    }
}