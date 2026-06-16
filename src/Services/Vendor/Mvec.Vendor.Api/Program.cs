using Mvec.BuildingBlocks.Web;
using Mvec.Vendor.Api.Application;
using Mvec.Vendor.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvecDefaults(builder.Configuration);
builder.Services.AddVendorInfrastructure(builder.Configuration);
builder.Services.AddVendorApplication();

var app = builder.Build();

app.UseMvecPipeline();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
