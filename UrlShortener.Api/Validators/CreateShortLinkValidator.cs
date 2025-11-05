using FluentValidation;
using UrlShortener.Api.DTOs;

namespace UrlShortener.Api.Validators
{
    public class CreateShortLinkValidator : AbstractValidator<CreateShortLinkDto>
    {
        public CreateShortLinkValidator()
        {
            RuleFor(x => x.OriginalUrl)
                .NotEmpty().WithMessage("Original URL is required")
                .MaximumLength(2048).WithMessage("URL too long")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("Invalid URL format");
        }
    }
}
