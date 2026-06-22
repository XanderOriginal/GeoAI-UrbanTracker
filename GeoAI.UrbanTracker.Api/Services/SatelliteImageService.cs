using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GeoAI.UrbanTracker.Api.Configuration;
using GeoAI.UrbanTracker.Api.Models;
using Microsoft.Extensions.Options;

namespace GeoAI.UrbanTracker.Api.Services;

public class SatelliteImageService : ISatelliteImageService
{
    private readonly HttpClient _httpClient;
    private readonly SentinelHubOptions _options;
    private readonly ILogger<SatelliteImageService> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;

    public SatelliteImageService(
        HttpClient httpClient,
        IOptions<SentinelHubOptions> options,
        ILogger<SatelliteImageService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SatelliteImage> FetchImageAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        DateOnly date,
        bool isBeforeImage,
        Guid analysisRequestId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var bbox = CalculateBbox(latitude, longitude, radiusMeters);
        var imageBytes = await FetchSentinelImageAsync(bbox, date, cancellationToken);

        var fileName = $"{analysisRequestId}_{(isBeforeImage ? "before" : "after")}_{date:yyyy-MM-dd}.png";
        var directory = Path.Combine("wwwroot", "images");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);

        _logger.LogInformation("Saved satellite image to {FilePath}", filePath);

        return new SatelliteImage
        {
            Id = Guid.NewGuid(),
            AnalysisRequestId = analysisRequestId,
            IsBeforeImage = isBeforeImage,
            CaptureDate = date,
            FilePath = filePath,
            CloudCoveragePercent = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt)
            return;

        _logger.LogInformation("Fetching Sentinel Hub access token...");

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _options.ClientId),
            new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
        });

        var response = await _httpClient.PostAsync(
            "https://identity.dataspace.copernicus.eu/auth/realms/CDSE/protocol/openid-connect/token",
            tokenRequest,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenData = JsonSerializer.Deserialize<JsonElement>(json);

        _accessToken = tokenData.GetProperty("access_token").GetString();
        var expiresIn = tokenData.GetProperty("expires_in").GetInt32();
        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 30);

        _logger.LogInformation("Sentinel Hub token acquired, expires in {ExpiresIn}s", expiresIn);
    }

    private async Task<byte[]> FetchSentinelImageAsync(
        (double minLng, double minLat, double maxLng, double maxLat) bbox,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var requestBody = new
        {
            input = new
            {
                bounds = new
                {
                    bbox = new[] { bbox.minLng, bbox.minLat, bbox.maxLng, bbox.maxLat },
                    properties = new { crs = "http://www.opengis.net/def/crs/OGC/1.3/CRS84" }
                },
                data = new[]
                {
                    new
                    {
                        type = "sentinel-2-l2a",
                        dataFilter = new
                        {
                            timeRange = new
                            {
                                from = $"{dateStr}T00:00:00Z",
                                to = $"{dateStr}T23:59:59Z"
                            },
                            maxCloudCoverage = 30
                        }
                    }
                }
            },
            output = new
            {
                width = 512,
                height = 512,
                responses = new[]
                {
                    new { identifier = "default", format = new { type = "image/png" } }
                }
            },
            evalscript = "//VERSION=3\nfunction setup() { return { input: ['B04','B03','B02'], output: { bands: 3 } }; }\nfunction evaluatePixel(s) { return [3.5*s.B04, 3.5*s.B03, 3.5*s.B02]; }"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.PostAsync(
            "https://sh.dataspace.copernicus.eu/api/v1/process",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private static (double minLng, double minLat, double maxLng, double maxLat) CalculateBbox(
        double latitude,
        double longitude,
        int radiusMeters)
    {
        // ~111km per degree
        const double metersPerDegree = 111_000.0;
        var deltaLat = radiusMeters / metersPerDegree;
        var deltaLng = radiusMeters / (metersPerDegree * Math.Cos(latitude * Math.PI / 180));

        return (
            minLng: longitude - deltaLng,
            minLat: latitude - deltaLat,
            maxLng: longitude + deltaLng,
            maxLat: latitude + deltaLat
        );
    }
}