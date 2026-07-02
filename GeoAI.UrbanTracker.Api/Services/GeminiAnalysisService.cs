using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GeoAI.UrbanTracker.Api.Configuration;
using GeoAI.UrbanTracker.Api.Helpers.ImageProcessing;
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

        var beforeBytes = await ImageSourceLoader.LoadBytesAsync(_httpClient, beforeImage.FilePath, cancellationToken);
        var afterBytes = await ImageSourceLoader.LoadBytesAsync(_httpClient, afterImage.FilePath, cancellationToken);

        var beforeBase64 = Convert.ToBase64String(beforeBytes);
        var afterBase64 = Convert.ToBase64String(afterBytes);

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

        // Retry з exponential backoff для тимчасових помилок Gemini API
        // 503 Service Unavailable, 429 Too Many Requests, 500/502/504
        const int maxAttempts = 4;
        HttpResponseMessage response = null!;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            // HttpContent не можна reuse після Send — створюємо новий щоразу
            var retryContent = new StringContent(json, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync(url, retryContent, cancellationToken);

            if (response.IsSuccessStatusCode) break;

            int status = (int)response.StatusCode;
            bool isTransient = status is 429 or 500 or 502 or 503 or 504;

            if (!isTransient || attempt == maxAttempts)
            {
                var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error {Status} after {Attempt} attempt(s): {Body}",
                    status, attempt, errBody);
                response.EnsureSuccessStatusCode();
            }

            // Exponential backoff: attempt 1→2s, 2→4s, 3→8s
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            _logger.LogWarning(
                "Gemini returned {Status}, attempt {Attempt}/{Max} — retrying in {Delay}s...",
                status, attempt, maxAttempts, delay.TotalSeconds);
            await Task.Delay(delay, cancellationToken);
        }

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