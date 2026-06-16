using Mvec.BuildingBlocks.Web;
using Mvec.Identity.Api.Application;
using Mvec.Identity.Api.Infrastructure;
using Mvec.Identity.Api.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvecDefaults(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddIdentityApplication();

var app = builder.Build();

// Apply migrations + seed the bootstrap admin (idempotent).
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    await seeder.SeedAsync();
}

app.UseMvecPipeline();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
