using Cloudweather.Temperature.DataAccess;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TemperatureDbContext>(opts =>
{
    opts.EnableSensitiveDataLogging()
        .EnableDetailedErrors()
        .UseNpgsql(builder.Configuration.GetConnectionString("AppDb"));
}, ServiceLifetime.Transient);

var app = builder.Build();

app.Run();