namespace GeoAI.UrbanTracker.Api.Models;

public class AnalysisRequest
{
    public Guid Id { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; }

    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }

    public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending;
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Навігаційні властивості (зв'язки для EF Core)
    public ICollection<SatelliteImage> SatelliteImages { get; set; } = new List<SatelliteImage>();
    public AnalysisResult? Result { get; set; }
}

public enum AnalysisStatus
{
    Pending,
    FetchingImages,
    Processing,
    AnalyzingWithAi,
    Completed,
    Failed
}