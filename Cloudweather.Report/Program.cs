using Cloudweather.Report.DataAccess;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WeatherReportDbContext>(opts =>
{
    opts.EnableSensitiveDataLogging()
        .EnableDetailedErrors()
        .UseNpgsql(builder.Configuration.GetConnectionString("AppDb"));
}, ServiceLifetime.Transient);

var app = builder.Build();

app.Run();