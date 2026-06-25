using GeoAI.UrbanTracker.Api.Data;
using GeoAI.UrbanTracker.Api.DTOs;
using GeoAI.UrbanTracker.Api.Models;
using GeoAI.UrbanTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoAI.UrbanTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAnalysisOrchestratorService _orchestrator;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(
        AppDbContext db,
        IAnalysisOrchestratorService orchestrator,
        ILogger<AnalysisController> logger)
    {
        _db = db;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<AnalysisResultDto>> CreateAnalysis(
        [FromBody] CreateAnalysisRequestDto dto,
        CancellationToken cancellationToken)
    {
        var request = new AnalysisRequest
        {
            Id = Guid.NewGuid(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            RadiusMeters = dto.RadiusMeters,
            DateFrom = dto.DateFrom,
            DateTo = dto.DateTo,
            Status = AnalysisStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.AnalysisRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);

        var result = await _orchestrator.RunAnalysisAsync(request.Id, cancellationToken);

        return Ok(MapToDto(request, result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AnalysisResultDto>> GetAnalysis(
    Guid id, CancellationToken cancellationToken)
    {
        var request = await _db.AnalysisRequests
            .Include(r => r.Result)
            .Include(r => r.SatelliteImages)  // ← додай
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null) return NotFound();
        return Ok(MapToDto(request, request.Result));
    }

    [HttpGet]
    public async Task<ActionResult<List<AnalysisResultDto>>> GetAllAnalyses(
        CancellationToken cancellationToken)
    {
        var requests = await _db.AnalysisRequests
            .Include(r => r.Result)
            .Include(r => r.SatelliteImages)  // ← додай
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(requests.Select(r => MapToDto(r, r.Result)).ToList());
    }

    private static AnalysisResultDto MapToDto(AnalysisRequest request, AnalysisResult? result)
    {
        var beforeImage = request.SatelliteImages
            .FirstOrDefault(i => i.IsBeforeImage);
        var afterImage = request.SatelliteImages
            .FirstOrDefault(i => !i.IsBeforeImage);

        string? BuildImageUrl(string? filePath)
        {
            if (filePath == null) return null;
            var filename = Path.GetFileName(filePath);
            return $"/images/{filename}";
        }

        return new AnalysisResultDto
        {
            RequestId = request.Id,
            Status = request.Status,
            ErrorMessage = request.ErrorMessage,
            NdviChangePercent = result?.NdviChangePercent,
            BuiltUpAreaChangePercent = result?.BuiltUpAreaChangePercent,
            GreenAreaChangePercent = result?.GreenAreaChangePercent,
            GeminiSummary = result?.GeminiSummary,
            CreatedAt = request.CreatedAt,
            CompletedAt = request.CompletedAt,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusMeters = request.RadiusMeters,
            BeforeImageUrl = BuildImageUrl(beforeImage?.FilePath),
            AfterImageUrl = BuildImageUrl(afterImage?.FilePath),
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
        };
    }
}