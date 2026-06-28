namespace GeoAI.UrbanTracker.Api.DTOs;

public class AnalysisResultDto
{
    public Guid RequestId { get; set; }

   
    public int Status { get; set; }

    public string? ErrorMessage { get; set; }

    public double? NdviChangePercent { get; set; }
    public double? BuiltUpAreaChangePercent { get; set; }
    public double? GreenAreaChangePercent { get; set; }
    public string? GeminiSummary { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? RadiusMeters { get; set; }

    public string? BeforeImageUrl { get; set; }
    public string? AfterImageUrl { get; set; }

    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}