using Mvec.Admin.Api.Application;
using Mvec.Admin.Api.Infrastructure;
using Mvec.BuildingBlocks.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvecDefaults(builder.Configuration);
builder.Services.AddAdminInfrastructure(builder.Configuration);
builder.Services.AddAdminApplication();

var app = builder.Build();

app.UseMvecPipeline();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
