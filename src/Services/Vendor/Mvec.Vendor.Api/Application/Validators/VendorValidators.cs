using FluentValidation;
using Mvec.Vendor.Api.Application.Contracts;

namespace Mvec.Vendor.Api.Application.Validators;

public sealed class RegisterVendorRequestValidator : AbstractValidator<RegisterVendorRequest>
{
    public RegisterVendorRequestValidator()
    {
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BusinessType).MaximumLength(50);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.ContactPhone).MaximumLength(32);
        RuleFor(x => x.Pan).MaximumLength(15);
        RuleFor(x => x.Gstin).MaximumLength(20);
    }
}

public sealed class UploadKycRequestValidator : AbstractValidator<UploadKycRequest>
{
    public UploadKycRequestValidator()
    {
        RuleFor(x => x.DocType).IsInEnum();
        RuleFor(x => x.BlobUrl).NotEmpty().MaximumLength(500);
    }
}

public sealed class RejectVendorRequestValidator : AbstractValidator<RejectVendorRequest>
{
    public RejectVendorRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
