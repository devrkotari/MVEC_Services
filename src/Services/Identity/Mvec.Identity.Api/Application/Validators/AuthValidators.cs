using FluentValidation;
using Mvec.Identity.Api.Application.Contracts;
using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Application.Validators;

/// <summary>Password policy AC-001: min 8 chars, upper + lower + digit.</summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).MaximumLength(32);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.UserType)
            .IsInEnum()
            .Must(t => t is UserType.Buyer or UserType.Vendor)
            .WithMessage("Self-registration is only allowed for Buyer or Vendor accounts.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class TwoFactorLoginRequestValidator : AbstractValidator<TwoFactorLoginRequest>
{
    public TwoFactorLoginRequestValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$");
    }
}

public sealed class SocialLoginRequestValidator : AbstractValidator<SocialLoginRequest>
{
    public SocialLoginRequestValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
        RuleFor(x => x.UserType).IsInEnum();
    }
}

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty();
    }
}
