using Cloudweather.Temperature.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TemperatureDbContext>(opts =>
{
    opts.EnableSensitiveDataLogging()
        .EnableDetailedErrors()
        .UseNpgsql(builder.Configuration.GetConnectionString("AppDb"));
}, ServiceLifetime.Transient);

var app = builder.Build();

app.MapGet("/observation/{zip}", async(string zip, [FromQuery] int? days, TemperatureDbContext dbContext) =>
{
    if (days is null or < 1 or > 30)
    {
        return Results.BadRequest("Please provide a valid number of days between 1 and 30");
    }
    
    var startData = DateTime.UtcNow - TimeSpan.FromDays(days.Value);
    var results = await dbContext.Temperature
        .Where(p => p.ZipCode == zip && p.CreatedOn > startData)
        .ToListAsync();
    
    return Results.Ok(results);

});

app.MapPost("/observation", async (Temperature temperature, TemperatureDbContext dbcontext) =>
{
    temperature.CreatedOn = temperature.CreatedOn.ToUniversalTime();
    await dbcontext.AddAsync(temperature);
    await dbcontext.SaveChangesAsync();
});

app.Run();