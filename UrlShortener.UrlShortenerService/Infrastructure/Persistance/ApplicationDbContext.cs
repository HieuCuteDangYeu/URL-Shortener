using Microsoft.EntityFrameworkCore;
using UrlShortener.UrlShortenerService.Domain.Entities;

namespace UrlShortener.UrlShortenerService.Infrastructure.Persistance;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ShortLink> ShortLinks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShortLink>().HasIndex(e => e.ShortCode).IsUnique();
    }
}
