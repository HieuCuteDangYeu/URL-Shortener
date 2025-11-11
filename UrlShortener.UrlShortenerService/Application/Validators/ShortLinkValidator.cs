using FluentValidation;
using UrlShortener.Shared.Protos;

namespace UrlShortener.UrlShortenerService.Application.Validators
{
    public class CreateShortLinkRequestValidator : AbstractValidator<CreateShortLinkRequest>
    {
        public CreateShortLinkRequestValidator()
        {
            RuleFor(x => x.OriginalUrl)
                .NotEmpty().WithMessage("OriginalUrl is required")
                .MaximumLength(2048)
                .Must(BeAValidUrl).WithMessage("OriginalUrl must be a valid http/https URL");

            RuleFor(x => x.CustomCode)
                .Length(6).When(x => !string.IsNullOrEmpty(x.CustomCode))
                .Matches("^[a-zA-Z0-9_-]+$").When(x => !string.IsNullOrEmpty(x.CustomCode))
                .WithMessage("CustomCode must be 3-10 chars, letters/numbers/_/- only");
        }

        private bool BeAValidUrl(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var u) &&
            (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
    }
}
