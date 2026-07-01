using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using GeoAI.UrbanTracker.Api.Configuration;
using Microsoft.Extensions.Options;

namespace GeoAI.UrbanTracker.Api.Services;

public class CloudinaryStorageService : ICloudStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryStorageService> _logger;

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
        using var stream = new MemoryStream(imageBytes);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            PublicId = $"geoai/{Path.GetFileNameWithoutExtension(fileName)}",
            Overwrite = true,
            Folder = "geoai-urbantracker",
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error != null)
            throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

        _logger.LogInformation("Uploaded image to Cloudinary: {Url}", result.SecureUrl);
        return result.SecureUrl.ToString();
    }
}