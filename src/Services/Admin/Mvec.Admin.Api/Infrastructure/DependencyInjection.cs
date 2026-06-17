using Mvec.Admin.Api.Application.Abstractions;
using Mvec.Admin.Api.Application.Options;
using Mvec.Admin.Api.Infrastructure.Clients;
using Mvec.Admin.Api.Infrastructure.Http;

namespace Mvec.Admin.Api.Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddAdminInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        var endpoints = config.GetSection(ServiceEndpointsOptions.SectionName).Get<ServiceEndpointsOptions>()
                        ?? new ServiceEndpointsOptions();

        services.AddHttpContextAccessor();
        services.AddTransient<BearerForwardingHandler>();

        // Typed clients call the downstream services directly (service-to-service) and forward the
        // caller's JWT via BearerForwardingHandler. No database is used by this BFF.
        services.AddHttpClient<IIdentityAdminClient, IdentityAdminClient>(c =>
                c.BaseAddress = BaseUri(endpoints.Identity, "http://localhost:5180"))
            .AddHttpMessageHandler<BearerForwardingHandler>();

        services.AddHttpClient<IVendorAdminClient, VendorAdminClient>(c =>
                c.BaseAddress = BaseUri(endpoints.Vendor, "http://localhost:5190"))
            .AddHttpMessageHandler<BearerForwardingHandler>();

        return services;
    }

    // Ensures a trailing slash so relative request paths ("api/...") combine correctly.
    private static Uri BaseUri(string? configured, string fallback)
    {
        var url = string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();
        if (!url.EndsWith('/')) url += "/";
        return new Uri(url);
    }
}
