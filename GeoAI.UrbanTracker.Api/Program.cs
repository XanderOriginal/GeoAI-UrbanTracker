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

builder.Services.Configure<CloudinaryOptions>(
    builder.Configuration.GetSection(CloudinaryOptions.SectionName));

builder.Services.AddScoped<ICloudStorageService, CloudinaryStorageService>();

builder.Services.AddHttpClient<SatelliteImageService>();
builder.Services.AddScoped<ISatelliteImageService, SatelliteImageService>();

builder.Services.AddHttpClient<GeminiAnalysisService>();
builder.Services.AddScoped<IGeminiAnalysisService, GeminiAnalysisService>();

builder.Services.AddScoped<IImageDiffService, ImageDiffService>();
builder.Services.AddScoped<IAnalysisOrchestratorService, AnalysisOrchestratorService>();



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:5500",
                "http://127.0.0.1:5500",
                "https://geo-ai-urban-tracker.vercel.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        
        opts.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
    });

builder.Services.AddOpenApi();

Directory.CreateDirectory(Path.Combine("wwwroot", "images"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();