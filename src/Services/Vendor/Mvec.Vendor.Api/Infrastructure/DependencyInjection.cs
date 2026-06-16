using MassTransit;
using Microsoft.EntityFrameworkCore;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Vendor.Api.Application.Abstractions;
using Mvec.Vendor.Api.Infrastructure.Messaging;
using Mvec.Vendor.Api.Infrastructure.Repositories;
using Mvec.Vendor.Api.Infrastructure.Security;

namespace Mvec.Vendor.Api.Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddVendorInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // ---- Persistence ----
        services.AddDbContext<VendorDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("VendorDb")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<VendorDbContext>());

        // ---- Repositories ----
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IVendorStoreRepository, VendorStoreRepository>();

        // ---- Security ----
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // ---- Messaging (direct publish; outbox not used yet) ----
        services.AddMessaging(config);
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        return services;
    }

    private static void AddMessaging(this IServiceCollection services, IConfiguration config)
    {
        var serviceBus = config["AzureServiceBus:ConnectionString"];

        services.AddMassTransit(x =>
        {
            if (string.IsNullOrWhiteSpace(serviceBus))
            {
                // No broker configured (local dev / tests): in-memory transport keeps the app runnable.
                x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
            }
            else
            {
                x.UsingAzureServiceBus((ctx, cfg) =>
                {
                    cfg.Host(serviceBus);
                    cfg.ConfigureEndpoints(ctx);
                });
            }
        });
    }
}
