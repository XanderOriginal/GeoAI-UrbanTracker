using GeoAI.UrbanTracker.Api.Models;

namespace GeoAI.UrbanTracker.Api.Services;

public interface ISatelliteImageService
{
    Task<SatelliteImage> FetchImageAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        DateOnly date,
        bool isBeforeImage,
        Guid analysisRequestId,
        CancellationToken cancellationToken = default);
}