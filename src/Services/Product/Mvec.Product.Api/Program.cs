using Microsoft.EntityFrameworkCore;
using Mvec.BuildingBlocks.Web;
using Mvec.Product.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvecDefaults(builder.Configuration);
builder.Services.AddDbContext<ProductDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ProductDb")));
// TODO: builder.Services.AddMvecMessaging(builder.Configuration);

var app = builder.Build();
app.UseMvecPipeline();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();
