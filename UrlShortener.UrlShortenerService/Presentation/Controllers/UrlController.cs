using Microsoft.AspNetCore.Mvc;
using UrlShortener.UrlShortenerService.Application.Interfaces;

namespace UrlShortener.UrlShortenerService.Presentation.Controllers;

[ApiController]
[Route("Url")]
public class UrlShortenerServiceController(IUrlShortenerService urlShortenerService) : ControllerBase
{
    [HttpGet("{shortCode}")]
    public async Task<IActionResult> RedirectToOriginalUrl(string shortCode)
    {
        var shortLink = await urlShortenerService.GetOriginalUrlAsync(shortCode);

        if (shortLink == null)
            return NotFound();

        await urlShortenerService.IncrementClickCountAsync(shortCode);

        return Redirect(shortLink.OriginalUrl);
    }
}   
