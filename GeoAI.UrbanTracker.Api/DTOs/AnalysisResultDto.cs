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
}