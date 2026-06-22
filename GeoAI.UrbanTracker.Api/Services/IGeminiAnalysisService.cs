using GeoAI.UrbanTracker.Api.Models;

namespace GeoAI.UrbanTracker.Api.Services;

public interface IGeminiAnalysisService
{
    Task<string> AnalyzeChangesAsync(
        SatelliteImage beforeImage,
        SatelliteImage afterImage,
        double ndviChange,
        double builtUpChange,
        CancellationToken cancellationToken = default);
}