using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.DTOs;
using UrlShortener.Api.Services;
using FluentValidation;

namespace UrlShortener.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShortLinkController(ShortLinkService service, IValidator<CreateShortLinkDto> validator) : ControllerBase
    {
        [HttpPost("shorten")]
        public async Task<IActionResult> Shorten([FromBody] CreateShortLinkDto dto)
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var result = await service.CreateShortLinkAsync(dto);
            return Ok(result);
        }

        [HttpGet("/u/{code}")]
        public async Task<IActionResult> RedirectToOriginal(string code)
        {
            var url = await service.ResolveShortCodeAsync(code);
            if (url == null) return NotFound("Short link not found");
            return Redirect(url);
        }
    }
}
