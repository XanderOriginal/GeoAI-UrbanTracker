using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using GeoAI.UrbanTracker.Api.Configuration;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace GeoAI.UrbanTracker.Api.Services;

public class CloudinaryStorageService : ICloudStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryStorageService> _logger;

    // Cloudinary Free plan hard limit — 10 485 760 bytes (10 MB).
    // Лишаємо запас (9.5 MB), бо реальна перевірка на боці Cloudinary
    // трохи консервативніша за заявлену цифру.
    private const long MaxUploadBytes = 9_500_000;

    public CloudinaryStorageService(
        IOptions<CloudinaryOptions> options,
        ILogger<CloudinaryStorageService> logger)
    {
        var opt = options.Value;
        var account = new Account(opt.CloudName, opt.ApiKey, opt.ApiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(
        byte[] imageBytes,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var (finalBytes, finalFileName) = CompressIfNeeded(imageBytes, fileName);

        using var stream = new MemoryStream(finalBytes);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(finalFileName, stream),
            PublicId = $"geoai/{Path.GetFileNameWithoutExtension(finalFileName)}",
            Overwrite = true,
            Folder = "geoai-urbantracker",
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error != null)
            throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

        _logger.LogInformation(
            "Uploaded image to Cloudinary: {Url} ({Bytes} bytes, original {OriginalBytes} bytes)",
            result.SecureUrl, finalBytes.Length, imageBytes.Length);
        return result.SecureUrl.ToString();
    }

    /// <summary>
    /// Sentinel-2 знімки 2500×2500 з alpha-каналом (маска хмар для ImageDiffService)
    /// у PNG часто важать 12-20 MB — понад ліміт Cloudinary Free (10 MB). WebP
    /// підтримує альфа-канал (на відміну від JPEG) і при lossy-компресії дає
    /// у 5-10 разів менший розмір без відчутної втрати для NDVI-аналізу, який
    /// і так семплує зображення з кроком 2 для сторін &gt;1024px.
    /// </summary>
    private (byte[] bytes, string fileName) CompressIfNeeded(byte[] imageBytes, string fileName)
    {
        if (imageBytes.Length <= MaxUploadBytes)
            return (imageBytes, fileName);

        using var original = SKBitmap.Decode(imageBytes);
        if (original is null)
        {
            _logger.LogWarning(
                "Cannot decode image for compression ({Bytes} bytes) — uploading as-is, may fail on Cloudinary",
                imageBytes.Length);
            return (imageBytes, fileName);
        }

        var webpFileName = Path.ChangeExtension(fileName, ".webp");

        // Крок 1: WebP lossy, поступово знижуємо якість
        foreach (var quality in new[] { 85, 70, 55, 40 })
        {
            using var image = SKImage.FromBitmap(original);
            using var encoded = image.Encode(SKEncodedImageFormat.Webp, quality);

            if (encoded.Size <= MaxUploadBytes)
            {
                _logger.LogInformation(
                    "Compressed {Original} → {Compressed} bytes (WebP q={Quality})",
                    imageBytes.Length, encoded.Size, quality);
                return (encoded.ToArray(), webpFileName);
            }
        }

        // Крок 2: якщо навіть WebP q=40 замало — зменшуємо роздільність вдвічі
        var scaledWidth = Math.Max(1, original.Width / 2);
        var scaledHeight = Math.Max(1, original.Height / 2);

        using var scaledBitmap = original.Resize(
            new SKImageInfo(scaledWidth, scaledHeight, original.ColorType, original.AlphaType),
            SKFilterQuality.Medium);

        if (scaledBitmap != null)
        {
            using var scaledImage = SKImage.FromBitmap(scaledBitmap);
            using var encoded = scaledImage.Encode(SKEncodedImageFormat.Webp, 65);

            _logger.LogInformation(
                "Compressed {Original} → {Compressed} bytes (downscaled to {W}x{H}, WebP q=65)",
                imageBytes.Length, encoded.Size, scaledWidth, scaledHeight);
            return (encoded.ToArray(), webpFileName);
        }

        _logger.LogWarning("Compression fallback exhausted — uploading original bytes as-is");
        return (imageBytes, fileName);
    }
}