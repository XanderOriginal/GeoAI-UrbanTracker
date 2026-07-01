namespace GeoAI.UrbanTracker.Api.Services;

public interface ICloudStorageService
{
    Task<string> UploadImageAsync(
        byte[] imageBytes,
        string fileName,
        CancellationToken cancellationToken = default);
}