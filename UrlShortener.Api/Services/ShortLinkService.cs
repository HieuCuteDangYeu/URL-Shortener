using UrlShortener.Api.DTOs;
using UrlShortener.Core.Entities;
using UrlShortener.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace UrlShortener.Api.Services
{
    public class ShortLinkService(AppDbContext context)
    {
        public async Task<ShortLinkResponseDto> CreateShortLinkAsync(CreateShortLinkDto dto)
        {
            // Check duplicates
            var existing = await context.ShortLinks
                .FirstOrDefaultAsync(x => x.OriginalUrl == dto.OriginalUrl);

            if (existing != null)
            {
                return new ShortLinkResponseDto
                {
                    OriginalUrl = existing.OriginalUrl,
                    ShortUrl = $"https://localhost:7243/u/{existing.ShortCode}"
                };
            }

            var shortCode = GenerateBeautifulCode();

            var entity = new ShortLink
            {
                OriginalUrl = dto.OriginalUrl,
                ShortCode = shortCode
            };

            context.ShortLinks.Add(entity);
            await context.SaveChangesAsync();

            return new ShortLinkResponseDto
            {
                OriginalUrl = dto.OriginalUrl,
                ShortUrl = $"https://localhost:7243/u/{shortCode}"
            };
        }

        public async Task<string?> ResolveShortCodeAsync(string code)
        {
            var link = await context.ShortLinks.FirstOrDefaultAsync(x => x.ShortCode == code);
            if (link == null) return null;

            link.Clicks++;
            await context.SaveChangesAsync();
            return link.OriginalUrl;
        }

        private static string GenerateBeautifulCode()
        {
            // Make code short, readable, and beautiful
            var random = Path.GetRandomFileName().Replace(".", "")[..6];
            return random;
        }
    }
}
