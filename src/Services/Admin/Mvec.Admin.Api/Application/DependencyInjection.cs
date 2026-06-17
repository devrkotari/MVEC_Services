using Mvec.Admin.Api.Application.Abstractions;
using Mvec.Admin.Api.Application.Services;

namespace Mvec.Admin.Api.Application;

public static class ApplicationModule
{
    public static IServiceCollection AddAdminApplication(this IServiceCollection services)
    {
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        return services;
    }
}
