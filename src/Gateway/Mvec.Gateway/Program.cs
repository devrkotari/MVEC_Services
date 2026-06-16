using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Pick the environment-specific Ocelot config as a whole file. Loading ocelot.json + an override
// file together would merge the "Routes" arrays by index (fragile), so we select one outright:
// Development → localhost ports, everything else → container hostnames.
var ocelotFile = builder.Environment.IsDevelopment() ? "ocelot.Development.json" : "ocelot.json";
builder.Configuration.AddJsonFile(ocelotFile, optional: false, reloadOnChange: true);

// JWT bearer validation at the edge (shared "Jwt" config, same keys every service uses).
// Tokens are still forwarded downstream, where services do the fine-grained role authorization.
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"] ?? "mvec",
            ValidateAudience = true,
            ValidAudience = jwt["Audience"] ?? "mvec",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"] ?? string.Empty)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role"
        };
    });

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
    corsOrigins = ["http://localhost:4200"];

builder.Services.AddCors(o => o.AddPolicy("spa", p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddOcelot(builder.Configuration).AddPolly();

var app = builder.Build();
app.UseCors("spa");
app.UseWebSockets();
app.UseAuthentication();

// Edge auth gate: every /api/* request must carry a valid JWT, except the public allowlist
// (auth bootstrap, public storefront, anonymous catalog browsing). Role checks stay downstream.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    var gated = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
                && !HttpMethods.IsOptions(context.Request.Method)   // let CORS preflight through
                && !IsAnonymous(context.Request);

    if (gated && !(context.User.Identity?.IsAuthenticated ?? false))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await next();
});

await app.UseOcelot();
app.Run();

// Endpoints reachable without a token. Everything else under /api/* requires authentication.
static bool IsAnonymous(HttpRequest request)
{
    const StringComparison Ord = StringComparison.OrdinalIgnoreCase;
    var path = request.Path.Value ?? string.Empty;

    if (HttpMethods.IsPost(request.Method))
    {
        // Identity bootstrap — no token exists yet.
        return path.Equals("/api/identity/register", Ord)
            || path.Equals("/api/identity/login", Ord)
            || path.Equals("/api/identity/login/2fa", Ord)
            || path.Equals("/api/identity/refresh", Ord)
            || path.Equals("/api/identity/verify-email", Ord)
            || path.StartsWith("/api/identity/social/", Ord);
    }

    if (HttpMethods.IsGet(request.Method))
    {
        // Public catalog browsing (buyers shop without logging in).
        if (path.StartsWith("/api/products", Ord)
            || path.StartsWith("/api/categories", Ord)
            || path.StartsWith("/api/reviews", Ord))
            return true;

        // Public storefront page: /api/vendors/{id}/store
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments is ["api", "vendors", _, "store"];
    }

    return false;
}
