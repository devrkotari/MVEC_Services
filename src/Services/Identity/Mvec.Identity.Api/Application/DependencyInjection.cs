using FluentValidation;
using FluentValidation.AspNetCore;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Services;
using Mvec.Identity.Api.Application.Validators;

namespace Mvec.Identity.Api.Application;

public static class ApplicationModule
{
    /// <summary>Registers application services and request validators (with auto-validation on binding).</summary>
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IRoleService, RoleService>();

        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddFluentValidationAutoValidation();
        return services;
    }
}
