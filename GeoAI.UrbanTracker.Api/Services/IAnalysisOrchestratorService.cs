using GeoAI.UrbanTracker.Api.Models;

namespace GeoAI.UrbanTracker.Api.Services;

public interface IAnalysisOrchestratorService
{
    Task<AnalysisResult> RunAnalysisAsync(
        Guid analysisRequestId,
        CancellationToken cancellationToken = default);
}