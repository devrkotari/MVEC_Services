using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Mvec.BuildingBlocks.Web;

// Composition helpers shared by every service.
public static class ServiceDefaults
{
    /// <summary>Named CORS policy applied to every service. Origins come from the "Cors:AllowedOrigins" config array.</summary>
    public const string CorsPolicy = "MvecCors";

    // Fallback used when no origins are configured (local Angular dev server).
    private static readonly string[] DefaultCorsOrigins = ["http://localhost:4200"];

    public static IServiceCollection AddMvecDefaults(this IServiceCollection services, IConfiguration config)
    {
        // Serialize/deserialize enums as their string names (e.g. "Buyer", "Active") so the
        // API contract matches the SPA's string unions and JWT role claims, rather than raw ints.
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(AddJwtSwaggerSecurity);
        services.AddProblemDetails();
        services.AddHealthChecks();

        services.AddMvecCors(config);
        services.AddMvecJwtAuth(config);
        services.AddMvecAuthorization();
        return services;
    }

    public static WebApplication UseMvecPipeline(this WebApplication app)
    {
        app.UseMvecExceptionHandling();
        app.UseMvecCorrelationId();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // CORS must run before authentication so preflight (OPTIONS) requests get the headers.
        app.UseCors(CorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    private static IServiceCollection AddMvecCors(this IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is null || origins.Length == 0)
            origins = DefaultCorsOrigins;

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials())); // required so the httpOnly refresh-token cookie is allowed cross-origin
        return services;
    }

    private static void AddJwtSwaggerSecurity(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
        const string scheme = "Bearer";
        options.AddSecurityDefinition(scheme, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter the JWT access token (without the 'Bearer ' prefix)."
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = scheme }
            }] = Array.Empty<string>()
        });
    }
}
