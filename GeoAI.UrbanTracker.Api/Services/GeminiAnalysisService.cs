using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GeoAI.UrbanTracker.Api.Configuration;
using GeoAI.UrbanTracker.Api.Models;
using Microsoft.Extensions.Options;

namespace GeoAI.UrbanTracker.Api.Services;

public class GeminiAnalysisService : IGeminiAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiAnalysisService> _logger;

    public GeminiAnalysisService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiAnalysisService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> AnalyzeChangesAsync(
        SatelliteImage beforeImage,
        SatelliteImage afterImage,
        double ndviChange,
        double builtUpChange,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending images to Gemini for analysis...");

        var beforeBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(beforeImage.FilePath, cancellationToken));
        var afterBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(afterImage.FilePath, cancellationToken));

        var prompt = $"""
            You are an expert urban analyst. You are given two satellite images of the same location taken at different times.
            
            Before image date: {beforeImage.CaptureDate:yyyy-MM-dd}
            After image date: {afterImage.CaptureDate:yyyy-MM-dd}
            
            Computed metrics:
            - NDVI change: {ndviChange:+0.00;-0.00}% (negative = vegetation loss, positive = vegetation gain)
            - Built-up area change: {builtUpChange:+0.00;-0.00}% (positive = more construction)
            
            Please analyze the two satellite images and provide:
            1. A summary of the main visible changes between the two images
            2. Assessment of urban development (new buildings, roads, construction sites)
            3. Assessment of green area changes (deforestation, new parks, vegetation loss/gain)
            4. Overall environmental impact assessment
            
            Be concise and factual. Response in 150-200 words.
            """;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "image/png",
                                data = beforeBase64
                            }
                        },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "image/png",
                                data = afterBase64
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        _httpClient.DefaultRequestHeaders.Remove("x-goog-api-key");
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _options.ApiKey);

        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

        var summary = responseData
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "No analysis available.";

        _logger.LogInformation("Gemini analysis completed successfully");
        return summary;
    }
}