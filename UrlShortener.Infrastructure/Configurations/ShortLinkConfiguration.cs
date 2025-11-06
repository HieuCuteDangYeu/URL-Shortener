using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Core.Entities;

namespace UrlShortener.Infrastructure.Configurations
{
    public class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
    {
        public void Configure(EntityTypeBuilder<ShortLink> b)
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.OriginalUrl).IsRequired().HasMaxLength(2048);
            b.Property(s => s.ShortCode).IsRequired().HasMaxLength(100);
            b.HasIndex(s => s.ShortCode).IsUnique();
            b.HasIndex(s => s.OriginalUrl).HasDatabaseName("IX_ShortLinks_OriginalUrl").HasFilter(null);
        }
    }
}
