using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Shared.Protos;
using UrlShortener.UrlShortenerService.Application.Interfaces;
using UrlShortener.UrlShortenerService.Infrastructure.Persistance;

namespace UrlShortener.UrlShortenerService.Application.Services;

public class UrlShortenerService(ApplicationDbContext context, IValidator<CreateShortLinkRequest> validator) : IUrlShortenerService
{
    private const string Characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public async Task<Domain.Entities.ShortLink> CreateShortLinkAsync(string originalUrl, string? customCode = null, Guid? userId = null)
    {
        var dto = new CreateShortLinkRequest 
        { 
            OriginalUrl = originalUrl, 
            CustomCode = customCode ?? string.Empty,
            UserId = userId?.ToString() ?? string.Empty,
        };

        FluentValidation.Results.ValidationResult validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var shortCode = customCode ?? GenerateShortCode();

        if (!string.IsNullOrEmpty(customCode))
        {
            var exists = await context.ShortLinks.AnyAsync(x => x.ShortCode == customCode);
            if (exists)
                throw new InvalidOperationException("Custom code already exists");
        }

        var shortLink = new Domain.Entities.ShortLink
        {
            Id = Guid.NewGuid(),
            ShortCode = shortCode,
            OriginalUrl = originalUrl,
            CreatedAt = DateTime.UtcNow,
            ClickCount = 0,
            UserId = userId
        };
            
        context.ShortLinks.Add(shortLink);
        await context.SaveChangesAsync();

        return shortLink;
    }

    public async Task<Domain.Entities.ShortLink?> GetOriginalUrlAsync(string shortCode)
    {
        return await context.ShortLinks
            .FirstOrDefaultAsync(x => x.ShortCode == shortCode);
    }

    public async Task<(List<Domain.Entities.ShortLink> Links, int TotalCount)> GetAllLinksAsync(int page, int pageSize, Guid? userId = null)
    {
        var query = context.ShortLinks.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        var totalCount = await query.CountAsync();

        var links = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (links, totalCount);
    }

    public async Task IncrementClickCountAsync(string shortCode)
    {
        var link = await context.ShortLinks.FirstOrDefaultAsync(x => x.ShortCode == shortCode);
        if (link != null)
        {
            link.ClickCount++;
            await context.SaveChangesAsync();
        }
    }

    private static string GenerateShortCode()
    {
        var random = new Random();
        var shortCode = new char[6];

        for (int i = 0; i < 6; i++)
        {
            shortCode[i] = Characters[random.Next(Characters.Length)];
        }

        return new string(shortCode);
    }
}
