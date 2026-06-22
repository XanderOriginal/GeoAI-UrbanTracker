namespace GeoAI.UrbanTracker.Api.Services;

public interface IImageDiffService
{
    Task<ImageDiffResult> ComputeDiffAsync(
        string beforeImagePath,
        string afterImagePath,
        CancellationToken cancellationToken = default);
}

public record ImageDiffResult(
    double NdviBefore,
    double NdviAfter,
    double NdviChangePercent,
    double BuiltUpAreaChangePercent,
    double GreenAreaChangePercent);