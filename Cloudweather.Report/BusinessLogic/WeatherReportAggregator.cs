using System.Text.Json;
using Cloudweather.Report.Config;
using Cloudweather.Report.DataAccess;
using Cloudweather.Report.Models;
using Microsoft.Extensions.Options;

namespace Cloudweather.Report.BusinessLogic;


/// <summary>
/// Aggregates data from multiple external sources to build a single weather report.
/// </summary>
public interface IWeatherReportAggregator
{
    /// <summary>
    /// Builds and return a Weekly Weather Report.
    /// Persists WeeklyWeatherReport data.
    /// </summary>
    /// <param name="zip"></param>
    /// <param name="days"></param>
    /// <returns></returns>
    public Task<WeatherReport> BuildWeeklyReport(string zip, int days);
}

public class WeatherReportAggregator : IWeatherReportAggregator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeatherReportAggregator> _logger;
    private readonly WeatherDataConfig _weatherDataConfig;
    private readonly WeatherReportDbContext _dbContext;

    public WeatherReportAggregator(
        IHttpClientFactory httpClientFactory, 
        ILogger<WeatherReportAggregator> logger, 
        IOptions<WeatherDataConfig> weatherDataConfig, 
        WeatherReportDbContext dbContext)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _weatherDataConfig = weatherDataConfig.Value;
        _dbContext = dbContext;
    }

    public async Task<WeatherReport> BuildWeeklyReport(string zip, int days)
    {
        var httpClient = _httpClientFactory.CreateClient();
        
        var precipData = await FetchPrecipitationData(httpClient, zip, days);
        var totalSnow = GetTotalSnow(precipData);
        var totalRain = GetTotalRain(precipData);
        _logger.LogInformation(
            $"zip: {zip} over last {days} days: " + 
            $"total snow: {totalSnow}, rain: {totalRain}"
        );
        
        
        var tempData = await FetchTemperatureData(httpClient, zip, days);
        var averageHighTemp = tempData.Average(t => t.TempHighF);
        var averageLowTemp = tempData.Average(t => t.TempLowF);
        _logger.LogInformation(
            $"zip: {zip} over last {days} days: " + 
            $"lo temp: {averageLowTemp}, hi temp: {averageHighTemp}"
        );

        var weatherReport = new WeatherReport
        {
            AverageHighF = Math.Round(averageHighTemp, 1),
            AverageLowF = Math.Round(averageLowTemp, 1),
            RainfallTotalInches = totalRain,
            SnowTotalInches = totalSnow,
            ZipCode = zip,
            CreatedOn = DateTime.UtcNow

        };

        _dbContext.Add(weatherReport);
        await _dbContext.SaveChangesAsync();

        return weatherReport;

    }

    

    private async Task<List<TemperatureModel>> FetchTemperatureData(HttpClient httpClient, string zip, int days)
    {
        var endpoint = BuildTemperatureServiceEndpoint(zip, days);
        var temperatureRecords = await httpClient.GetAsync(endpoint);
        
        var jsonSerializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        
        var temperatureData = await temperatureRecords
            .Content
            .ReadFromJsonAsync<List<TemperatureModel>>(jsonSerializeOptions);

        return temperatureData ?? new List<TemperatureModel>();
    }
    
    private async Task<List<PrecipitationModel>> FetchPrecipitationData(HttpClient httpClient, string zip, int days)
    {
        var endpoint = BuildPrecipitationServiceEndpoint(zip, days);
        var precipRecords = await httpClient.GetAsync(endpoint);
        
        var jsonSerializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        var precipData = await precipRecords
            .Content
            .ReadFromJsonAsync<List<PrecipitationModel>>(jsonSerializeOptions);

        return precipData ?? new List<PrecipitationModel>();
    }
    
    private string? BuildTemperatureServiceEndpoint(string zip, int days)
    {
        var tempServiceProtocol = _weatherDataConfig.TempDataProtocol;
        var tempServiceHost = _weatherDataConfig.TempDataHost;
        var tempServicePort = _weatherDataConfig.TempDataPort;

        return $"{tempServiceProtocol}://{tempServiceHost}:{tempServicePort}/observation/{zip}?days={days}";
    }
    
    private string? BuildPrecipitationServiceEndpoint(string zip, int days)
    {
        var precipServiceProtocol = _weatherDataConfig.PrecipDataProtocol;
        var precipServiceHost = _weatherDataConfig.PrecipDataHost;
        var precipServicePort = _weatherDataConfig.PrecipDataPort;

        return $"{precipServiceProtocol}://{precipServiceHost}:{precipServicePort}/observation/{zip}?days={days}";
    }
    
    private static decimal GetTotalRain(IEnumerable<PrecipitationModel> precipData)
    {
        var totalRain = precipData
            .Where(p => p.WeatherType == "rain")
            .Sum(p => p.AmountInches);

        return Math.Round(totalRain, 1);
    }

    private static decimal GetTotalSnow(List<PrecipitationModel> precipData)
    {
        var totalSnow = precipData
            .Where(p => p.WeatherType == "snow")
            .Sum(p => p.AmountInches);

        return Math.Round(totalSnow, 1);
    }

}