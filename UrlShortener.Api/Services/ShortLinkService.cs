using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.DTOs;
using UrlShortener.Core.Entities;
using UrlShortener.Infrastructure;

namespace UrlShortener.Api.Services
{
    public class ShortLinkService(AppDbContext context, IConfiguration config)
    {
        private readonly string shortBaseUrl = config["Shortener:BaseUrl"]
                ?? throw new InvalidOperationException("Shortener:BaseUrl not configured.");

        public async Task<ShortLinkResponseDto> CreateShortLinkAsync(CreateShortLinkDto dto)
        {
            // Prevent shortening already shortened URLs
            if (dto.OriginalUrl.StartsWith(shortBaseUrl, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot shorten an already shortened URL.");

            // Parse and normalize
            if (!Uri.TryCreate(dto.OriginalUrl.Trim(), UriKind.Absolute, out var uri))
                throw new InvalidOperationException("Invalid URL format.");

            var normalizedUrl = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
            if (!string.IsNullOrEmpty(uri.Query))
                normalizedUrl += uri.Query;

            // Block local/internal URLs (to prevent SSRF)
            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.StartsWith("127.") || uri.Host.StartsWith("10.") || uri.Host.StartsWith("192.168."))
            {
                throw new InvalidOperationException("Local or internal URLs are not allowed.");
            }

            // Check duplicates
            var existing = await context.ShortLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OriginalUrl == dto.OriginalUrl);

            if (existing != null)
            {
                return new ShortLinkResponseDto
                {
                    OriginalUrl = existing.OriginalUrl,
                    ShortUrl = $"{shortBaseUrl}/u/{existing.ShortCode}"
                };
            }

            // Generate unique short code with retries
            const int maxAttempts = 5;
            for (int attempts = 0; attempts < maxAttempts; attempts++)
            {
                var shortCode = GenerateBeautifulCode();

                var entity = new ShortLink
                {
                    OriginalUrl = dto.OriginalUrl,
                    ShortCode = shortCode
                };

                context.ShortLinks.Add(entity);

                try
                {
                    await context.SaveChangesAsync();
                    return new ShortLinkResponseDto
                    {
                        OriginalUrl = dto.OriginalUrl,
                        ShortUrl = $"{shortBaseUrl}/u/{shortCode}"
                    };
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    // Collision occurred, retry with a new code
                    context.Entry(entity).State = EntityState.Detached;
                }
            }

            throw new InvalidOperationException("Failed to generate a unique short code after multiple attempts.");
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
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string([.. Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)])]);
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException != null &&
                   ex.InnerException.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
        }
    }
}
