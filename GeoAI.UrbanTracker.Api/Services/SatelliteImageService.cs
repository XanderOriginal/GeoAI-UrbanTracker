using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GeoAI.UrbanTracker.Api.Configuration;
using GeoAI.UrbanTracker.Api.Helpers.ImageProcessing;
using GeoAI.UrbanTracker.Api.Models;
using Microsoft.Extensions.Options;
using SkiaSharp;

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
        var imageBytes = await FetchSentinelImageAsync(bbox, radiusMeters, date, cancellationToken);

        var fileName = $"{analysisRequestId}_{(isBeforeImage ? "before" : "after")}_{date:yyyy-MM-dd}.png";
        var directory = Path.Combine("wwwroot", "images");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);

        _logger.LogInformation("Saved satellite image to {FilePath} ({Bytes} bytes)", filePath, imageBytes.Length);

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
        int radiusMeters,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        // Стратегія: сезонне вікно ±30 днів навколо дати.
        // Це зберігає сезонність (порівнюємо однакові місяці між роками)
        // і дає достатньо часу знайти чистий знімок.
        // Приклад: DateFrom=2020-06-01 → шукаємо 2020-05-02..2020-07-01
        //          DateTo=2023-06-01   → шукаємо 2023-05-02..2023-07-01
        var windowStart = date.AddDays(-30);
        var windowEnd = date.AddDays(30);

        // Роздільна здатність підлаштовується під розмір ділянки замість
        // фіксованих 512×512 — інакше великий радіус (наприклад 50 км) розмазувався
        // в кашу (~195 м/піксель), а малий радіус (500 м) даремно пересемплювався.
        var (outputWidth, outputHeight) = CalculateOutputResolution(radiusMeters);

        // Перша спроба: суворий поріг хмарності 10%
        var imageBytes = await TryFetchWithCloudLimit(
            bbox, windowStart, windowEnd, maxCloud: 10, outputWidth, outputHeight, cancellationToken);

        // Fallback: якщо за 60-денним вікном з 10% нічого — розширюємо до 20%
        if (imageBytes is null)
        {
            _logger.LogWarning(
                "No image with ≤10% cloud cover in window {From}–{To}. Retrying with ≤20%...",
                windowStart, windowEnd);
            imageBytes = await TryFetchWithCloudLimit(
                bbox, windowStart, windowEnd, maxCloud: 20, outputWidth, outputHeight, cancellationToken);
        }

        // Останній fallback: 35% і розширене вікно ±60 днів
        if (imageBytes is null)
        {
            var wideStart = date.AddDays(-60);
            var wideEnd = date.AddDays(60);
            _logger.LogWarning(
                "Still no clean image. Expanding window to {From}–{To} with ≤35% cloud cover...",
                wideStart, wideEnd);
            imageBytes = await TryFetchWithCloudLimit(
                bbox, wideStart, wideEnd, maxCloud: 35, outputWidth, outputHeight, cancellationToken);
        }

        if (imageBytes is null)
            throw new InvalidOperationException(
                $"No usable Sentinel-2 image found near {date:yyyy-MM-dd}. " +
                "Try a different date or location with less persistent cloud cover.");

        _logger.LogInformation("Received {Bytes} bytes for target date {Date}", imageBytes.Length, date);
        return imageBytes;
    }

    /// <summary>
    /// Розраховує вихідну роздільність так, щоб пікселі лишались близько до нативних
    /// ~10 м/піксель Sentinel-2 (B04/B03/B02) незалежно від обраного радіуса.
    /// Мінімум 512px гарантує достатньо вибірки для NDVI-статистики навіть на маленьких
    /// ділянках; максимум 2500px — стеля Sentinel Hub Process API для синхронного запиту.
    /// </summary>
    private static (int width, int height) CalculateOutputResolution(int radiusMeters)
    {
        double bboxSideMeters = radiusMeters * 2.0; // CalculateBbox будує квадрат ±radiusMeters
        const double targetMetersPerPixel = 10.0;

        int px = (int)Math.Round(bboxSideMeters / targetMetersPerPixel);
        px = Math.Clamp(px, 512, 2500);
        return (px, px);
    }

    private async Task<byte[]?> TryFetchWithCloudLimit(
        (double minLng, double minLat, double maxLng, double maxLat) bbox,
        DateOnly windowStart,
        DateOnly windowEnd,
        int maxCloud,
        int outputWidth,
        int outputHeight,
        CancellationToken cancellationToken)
    {
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
                                from = $"{windowStart:yyyy-MM-dd}T00:00:00Z",
                                to   = $"{windowEnd:yyyy-MM-dd}T23:59:59Z"
                            },
                            maxCloudCoverage = maxCloud,
                            mosaickingOrder  = "leastCC"
                        }
                    }
                }
            },
            output = new
            {
                width = outputWidth,
                height = outputHeight,
                responses = new[]
                {
                    new { identifier = "default", format = new { type = "image/png" } }
                }
            },
            evalscript = @"//VERSION=3
function setup() {
  return {
    input: [{ bands: ['B04', 'B03', 'B02', 'SCL', 'dataMask'] }],
    output: { bands: 4, sampleType: 'UINT8' }
  };
}
function evaluatePixel(sample) {
  var r = Math.min(255, Math.pow(Math.max(0, sample.B04), 0.7) * 400);
  var g = Math.min(255, Math.pow(Math.max(0, sample.B03), 0.7) * 400);
  var b = Math.min(255, Math.pow(Math.max(0, sample.B02), 0.7) * 400);

  // SCL: 0=no data, 3=cloud shadow, 8=cloud medium, 9=cloud high, 10=cirrus
  var isBadPixel = (sample.dataMask === 0) ||
                    sample.SCL === 3 || sample.SCL === 8 ||
                    sample.SCL === 9 || sample.SCL === 10;

  // 4-й канал — справжня alpha-прозорість PNG: 255 = валідний піксель (видимий),
  // 0 = хмара/тінь/no-data (прозорий). Це СТАНДАРТНА семантика alpha — інакше
  // будь-який рендерер (браузер, Gemini, попередній перегляд) показує валідні
  // пікселі прозорими, а хмари — суцільними, і зображення виглядає чорним.
  return [r, g, b, isBadPixel ? 0 : 255];
    }"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.PostAsync(
            "https://sh.dataspace.copernicus.eu/api/v1/process",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Sentinel Hub returned {Status} for cloud≤{Cloud}% window {From}–{To}: {Body}",
                response.StatusCode, maxCloud, windowStart, windowEnd, errorBody);

            // 400/422 зазвичай означає "немає знімків у цьому вікні" — не fatal
            if ((int)response.StatusCode is 400 or 422)
                return null;

            // Реальна помилка — кидаємо
            response.EnsureSuccessStatusCode();
        }

        var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        // ── Реальна перевірка якості замість колишньої евристики "< 100 KB = погано" ──
        // Стара евристика хибно відкидала ДОБРІ знімки: маленький радіус (напр. 500 м)
        // або однорідна територія (поле, ліс, водойма) дають низькоентропійний PNG,
        // який законно важить менше 100 KB — і знімок відкидався попри те, що з ним
        // усе гаразд. Натомість evalscript уже рахує справжню Scene Classification
        // Layer-маску (isBadPixel: хмара/тінь/no-data) і кладе її в 4-й канал (alpha)
        // вихідного PNG. Декодуємо зображення і дивимось на РЕАЛЬНУ частку поганих
        // пікселів у вибраній ділянці — це і точніше, і коректно працює на будь-якому
        // масштабі, бо maxCloudCoverage в Sentinel Hub фільтрує хмарність по всій сцені,
        // а не по конкретному кропу, який нас цікавить.
        using var bitmap = ImageDecoding.DecodeUnpremultiplied(imageBytes);
        if (bitmap is null)
        {
            _logger.LogWarning(
                "Failed to decode image for cloud≤{Cloud}% window {From}–{To} — treating as invalid",
                maxCloud, windowStart, windowEnd);
            return null;
        }

        int stepX = bitmap.Width > 1024 ? 2 : 1;
        int stepY = bitmap.Height > 1024 ? 2 : 1;
        long sampled = 0;
        long badPixels = 0;

        // alpha тепер стандартна: 255 = валідний (видимий), 0 = хмара/тінь/no-data (прозорий)
        for (int y = 0; y < bitmap.Height; y += stepY)
            for (int x = 0; x < bitmap.Width; x += stepX)
            {
                sampled++;
                if (bitmap.GetPixel(x, y).Alpha < 128) badPixels++;
            }

        double badRatio = sampled == 0 ? 1.0 : badPixels / (double)sampled;

        // Невеликий запас (5pp) на тіні/межі хмар навколо самих хмарних пікселів,
        // які SCL інколи класифікує не ідеально на кордоні.
        double allowedRatio = (maxCloud / 100.0) + 0.05;

        if (badRatio > allowedRatio || badRatio > 0.95)
        {
            _logger.LogWarning(
                "Crop has {BadPct:P1} cloud/shadow/no-data pixels (limit ≤{Allowed:P0}) for cloud≤{Cloud}% window {From}–{To} — rejecting",
                badRatio, allowedRatio, maxCloud, windowStart, windowEnd);
            return null;
        }

        _logger.LogInformation(
            "Got {Width}x{Height} image, {BadPct:P1} cloud/shadow/no-data, for cloud≤{Cloud}% window {From}–{To}",
            bitmap.Width, bitmap.Height, badRatio, maxCloud, windowStart, windowEnd);
        return imageBytes;
    }

    private static (double minLng, double minLat, double maxLng, double maxLat) CalculateBbox(
        double latitude,
        double longitude,
        int radiusMeters)
    {
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