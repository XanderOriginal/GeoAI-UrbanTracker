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

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();