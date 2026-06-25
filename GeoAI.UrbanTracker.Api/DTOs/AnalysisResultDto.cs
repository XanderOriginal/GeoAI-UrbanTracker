using GeoAI.UrbanTracker.Api.Models;

namespace GeoAI.UrbanTracker.Api.DTOs;

public class AnalysisResultDto
{
    public Guid RequestId { get; set; }
    public AnalysisStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    public double? NdviChangePercent { get; set; }
    public double? BuiltUpAreaChangePercent { get; set; }
    public double? GreenAreaChangePercent { get; set; }
    public string? GeminiSummary { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Координати для overlay знімків на карті
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? RadiusMeters { get; set; }

    // URL до знімків (для overlay на карті)
    public string? BeforeImageUrl { get; set; }
    public string? AfterImageUrl { get; set; }

    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}