using FluentValidation;
using Mvec.Vendor.Api.Application.Contracts;

namespace Mvec.Vendor.Api.Application.Validators;

public sealed class UpdateStoreRequestValidator : AbstractValidator<UpdateStoreRequest>
{
    public UpdateStoreRequestValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.LogoUrl).MaximumLength(500);
        RuleFor(x => x.BannerUrl).MaximumLength(500);
        RuleForEach(x => x.ShippingZones).ChildRules(z =>
        {
            z.RuleFor(s => s.ZoneName).NotEmpty().MaximumLength(100);
            z.RuleFor(s => s.FlatRate).GreaterThanOrEqualTo(0);
            z.RuleFor(s => s.FreeAbove).GreaterThanOrEqualTo(0).When(s => s.FreeAbove.HasValue);
        });
    }
}
