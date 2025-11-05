using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;
using UrlShortener.Infrastructure.Configurations;
using UrlShortener.Infrastructure.Entities;

namespace UrlShortener.Infrastructure
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
    {
        public DbSet<ShortLink> ShortLinks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new ShortLinkConfiguration());
        }
    }
}
