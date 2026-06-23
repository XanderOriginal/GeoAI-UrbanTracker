using GeoAI.UrbanTracker.Api.Data;
using GeoAI.UrbanTracker.Api.Configuration;
using GeoAI.UrbanTracker.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<SentinelHubOptions>(
    builder.Configuration.GetSection(SentinelHubOptions.SectionName));

builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection(GeminiOptions.SectionName));

builder.Services.AddHttpClient<SatelliteImageService>();
builder.Services.AddScoped<ISatelliteImageService, SatelliteImageService>();

builder.Services.AddHttpClient<GeminiAnalysisService>();
builder.Services.AddScoped<IGeminiAnalysisService, GeminiAnalysisService>();

builder.Services.AddScoped<IImageDiffService, ImageDiffService>();

builder.Services.AddScoped<IAnalysisOrchestratorService, AnalysisOrchestratorService>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
