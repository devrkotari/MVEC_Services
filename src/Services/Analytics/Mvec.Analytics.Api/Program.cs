using Microsoft.EntityFrameworkCore;
using Mvec.BuildingBlocks.Web;
using Mvec.Analytics.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvecDefaults(builder.Configuration);
builder.Services.AddDbContext<AnalyticsDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("AnalyticsDb")));
// TODO: builder.Services.AddMvecMessaging(builder.Configuration);

var app = builder.Build();
app.UseMvecPipeline();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();
