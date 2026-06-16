using MassTransit;
using Microsoft.EntityFrameworkCore;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Options;
using Mvec.Identity.Api.Infrastructure.Messaging;
using Mvec.Identity.Api.Infrastructure.Repositories;
using Mvec.Identity.Api.Infrastructure.Security;
using Mvec.Identity.Api.Infrastructure.Seed;
using Mvec.Identity.Api.Infrastructure.Social;

namespace Mvec.Identity.Api.Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // ---- Options ----
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<SocialAuthOptions>(config.GetSection(SocialAuthOptions.SectionName));
        services.Configure<AdminSeedOptions>(config.GetSection(AdminSeedOptions.SectionName));

        // ---- Persistence ----
        services.AddDbContext<IdentityDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("IdentityDb")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        // ---- Repositories ----
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IUserAddressRepository, UserAddressRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();

        // ---- Security primitives ----
        services.AddHttpContextAccessor();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // ---- Social login providers ----
        services.AddHttpClient<IExternalAuthValidator, GoogleAuthValidator>();
        services.AddHttpClient<IExternalAuthValidator, FacebookAuthValidator>();

        // ---- Messaging (direct publish; outbox not used yet) ----
        services.AddMessaging(config);
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        // ---- Startup seeding ----
        services.AddScoped<IdentitySeeder>();

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
