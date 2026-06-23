using GeoAI.UrbanTracker.Api.Data;
using GeoAI.UrbanTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GeoAI.UrbanTracker.Api.Services;

public class AnalysisOrchestratorService : IAnalysisOrchestratorService
{
    private readonly AppDbContext _db;
    private readonly ISatelliteImageService _satelliteImageService;
    private readonly IImageDiffService _imageDiffService;
    private readonly IGeminiAnalysisService _geminiAnalysisService;
    private readonly ILogger<AnalysisOrchestratorService> _logger;

    public AnalysisOrchestratorService(
        AppDbContext db,
        ISatelliteImageService satelliteImageService,
        IImageDiffService imageDiffService,
        IGeminiAnalysisService geminiAnalysisService,
        ILogger<AnalysisOrchestratorService> logger)
    {
        _db = db;
        _satelliteImageService = satelliteImageService;
        _imageDiffService = imageDiffService;
        _geminiAnalysisService = geminiAnalysisService;
        _logger = logger;
    }

    public async Task<AnalysisResult> RunAnalysisAsync(
        Guid analysisRequestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _db.AnalysisRequests
            .FirstOrDefaultAsync(r => r.Id == analysisRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"AnalysisRequest {analysisRequestId} not found");

        try
        {
            // завантажити знімки
            request.Status = AnalysisStatus.FetchingImages;
            await _db.SaveChangesAsync(cancellationToken);

            var beforeImage = await _satelliteImageService.FetchImageAsync(
                request.Latitude, request.Longitude, request.RadiusMeters,
                request.DateFrom, isBeforeImage: true,
                request.Id, cancellationToken);

            var afterImage = await _satelliteImageService.FetchImageAsync(
                request.Latitude, request.Longitude, request.RadiusMeters,
                request.DateTo, isBeforeImage: false,
                request.Id, cancellationToken);

            _db.SatelliteImages.AddRange(beforeImage, afterImage);
            await _db.SaveChangesAsync(cancellationToken);

            // порівняти знімки
            request.Status = AnalysisStatus.Processing;
            await _db.SaveChangesAsync(cancellationToken);

            var diff = await _imageDiffService.ComputeDiffAsync(
                beforeImage.FilePath, afterImage.FilePath, cancellationToken);

            // аналіз через Gemini
            request.Status = AnalysisStatus.AnalyzingWithAi;
            await _db.SaveChangesAsync(cancellationToken);

            var geminiSummary = await _geminiAnalysisService.AnalyzeChangesAsync(
                beforeImage, afterImage,
                diff.NdviChangePercent, diff.BuiltUpAreaChangePercent,
                cancellationToken);

            // зберегти результат
            var result = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                AnalysisRequestId = request.Id,
                NdviBefore = diff.NdviBefore,
                NdviAfter = diff.NdviAfter,
                NdviChangePercent = diff.NdviChangePercent,
                BuiltUpAreaChangePercent = diff.BuiltUpAreaChangePercent,
                GreenAreaChangePercent = diff.GreenAreaChangePercent,
                GeminiSummary = geminiSummary,
                CreatedAt = DateTime.UtcNow
            };

            _db.AnalysisResults.Add(result);

            request.Status = AnalysisStatus.Completed;
            request.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Analysis {Id} completed successfully", analysisRequestId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis {Id} failed", analysisRequestId);
            request.Status = AnalysisStatus.Failed;
            request.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}