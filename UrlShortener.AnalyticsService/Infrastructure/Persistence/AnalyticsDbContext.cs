using Microsoft.EntityFrameworkCore;
using UrlShortener.AnalyticsService.Domain.Entities;

namespace UrlShortener.AnalyticsService.Infrastructure.Persistence;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options) { }

    public DbSet<ClickRecord> ClickRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClickRecord>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ShortUrlId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Browser).HasMaxLength(128);
            b.Property(x => x.Device).HasMaxLength(128);
            b.Property(x => x.Ip).HasMaxLength(64);
            b.HasIndex(x => new { x.ShortUrlId, x.Timestamp });
            b.HasIndex(x => x.Timestamp);
            b.HasIndex(x => x.UserId);
        });
    }
}