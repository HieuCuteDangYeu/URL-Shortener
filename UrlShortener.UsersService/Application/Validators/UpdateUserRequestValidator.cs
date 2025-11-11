using FluentValidation;
using UrlShortener.UserService.Application.Requests;

namespace UrlShortener.UserService.Application.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    private static readonly string[] AllowedRoles = new[] { "User", "Admin" };

    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName != null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName != null);
        RuleFor(x => x.PhoneNumber).MaximumLength(32).When(x => x.PhoneNumber != null);
        RuleFor(x => x.Role).Must(r => r == null || AllowedRoles.Contains(r)).WithMessage("Role must be User or Admin");

        When(x => !string.IsNullOrEmpty(x.Password), () =>
        {
            RuleFor(x => x.Password)
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
        });
    }
}