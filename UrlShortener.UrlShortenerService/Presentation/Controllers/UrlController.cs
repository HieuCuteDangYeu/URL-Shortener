using Microsoft.AspNetCore.Mvc;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.Infrastructure.Messaging.Events;
using UrlShortener.UrlShortenerService.Application.Interfaces;

namespace UrlShortener.UrlShortenerService.Presentation.Controllers;

[ApiController]
[Route("Url")]
public class UrlShortenerServiceController(IUrlShortenerService urlShortenerService, IMessageBroker messageBroker) : ControllerBase
{
    [HttpGet("{shortCode}")]
    public async Task<IActionResult> RedirectToOriginalUrl(string shortCode)
    {
        var shortLink = await urlShortenerService.GetOriginalUrlAsync(shortCode);

        if (shortLink == null)
            return NotFound();

        var urlClickedEvent = new UrlClickedEvent
        {
            ShortCode = shortLink.ShortCode,
            CreatedAt = DateTime.UtcNow,
            UserId = shortLink.UserId?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            Referrer = Request.Headers.Referer.ToString()
        };

        messageBroker.Publish("url-clicked", urlClickedEvent);

        return Redirect(shortLink.OriginalUrl);
    }
}
