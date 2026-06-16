using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Mvec.BuildingBlocks.Web;

/// <summary>Role names minted into the JWT <c>role</c> claim by the Identity service.</summary>
public static class MvecRoles
{
    public const string Buyer = "Buyer";
    public const string Vendor = "Vendor";
    public const string Admin = "Admin";
}

public static class AuthExtensions
{
    /// <summary>
    /// Wires JWT bearer authentication using the shared "Jwt" section
    /// (Issuer / Audience / SigningKey). Used by every service to validate access tokens.
    /// </summary>
    public static IServiceCollection AddMvecJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var section = config.GetSection("Jwt");
        var issuer = section["Issuer"] ?? "mvec";
        var audience = section["Audience"] ?? "mvec";
        var signingKey = section["SigningKey"] ?? string.Empty;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep JWT claim names verbatim. Otherwise the handler's default inbound map
                // rewrites "role" → the long ".../identity/claims/role" URI, which no longer
                // matches RoleClaimType = "role" below, so RequireRole(...) returns 403.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = "role"
                };
            });

        return services;
    }

    /// <summary>Registers role-based authorization policies shared across services.</summary>
    public static IServiceCollection AddMvecAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(MvecRoles.Admin, p => p.RequireRole(MvecRoles.Admin))
            .AddPolicy(MvecRoles.Vendor, p => p.RequireRole(MvecRoles.Vendor))
            .AddPolicy(MvecRoles.Buyer, p => p.RequireRole(MvecRoles.Buyer));
        return services;
    }
}
