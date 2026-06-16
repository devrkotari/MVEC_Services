using FluentValidation;
using FluentValidation.AspNetCore;
using Mvec.Vendor.Api.Application.Abstractions;
using Mvec.Vendor.Api.Application.Services;
using Mvec.Vendor.Api.Application.Validators;

namespace Mvec.Vendor.Api.Application;

public static class ApplicationModule
{
    /// <summary>Registers application services and request validators (with auto-validation on binding).</summary>
    public static IServiceCollection AddVendorApplication(this IServiceCollection services)
    {
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IStoreService, StoreService>();

        services.AddValidatorsFromAssemblyContaining<RegisterVendorRequestValidator>();
        services.AddFluentValidationAutoValidation();
        return services;
    }
}
