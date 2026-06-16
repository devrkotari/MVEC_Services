using Microsoft.EntityFrameworkCore;
using Mvec.BuildingBlocks.Web;
using Mvec.Notification.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvecDefaults(builder.Configuration);
builder.Services.AddDbContext<NotificationDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("NotificationDb")));
// TODO: builder.Services.AddMvecMessaging(builder.Configuration);

var app = builder.Build();
app.UseMvecPipeline();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();
