using Cloudweather.Precipitation.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddDbContext<PrecipDbContext>(opts =>
{
    opts.EnableSensitiveDataLogging()
        .EnableDetailedErrors()
        .UseNpgsql(builder.Configuration.GetConnectionString("AppDb"));
}, ServiceLifetime.Transient);

var app = builder.Build();

app.MapGet("/observation/{zip}", async(string zip, [FromQuery] int? days, PrecipDbContext dbContext) =>
{
    if (days is null or < 1 or > 30)
    {
        return Results.BadRequest("Please provide a valid number of days between 1 and 30");
    }
    
    var startData = DateTime.UtcNow - TimeSpan.FromDays(days.Value);
    var results = await dbContext.Precipitation
        .Where(p => p.ZipCode == zip && p.CreatedOn > startData)
        .ToListAsync();
    
    return Results.Ok(results);

});

app.Run();

