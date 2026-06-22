namespace GeoAI.UrbanTracker.Api.Models;

public class SatelliteImage
{
    public Guid Id { get; set; }

    public Guid AnalysisRequestId { get; set; }
    public AnalysisRequest AnalysisRequest { get; set; } = null!;

    // Чи це знімок "до" чи "після"
    public bool IsBeforeImage { get; set; }

    public DateOnly CaptureDate { get; set; }

    // Шлях до файлу на диску/сховищі (не зберігаємо саме зображення в БД)
    public string FilePath { get; set; } = string.Empty;

    // Метадані з Sentinel Hub
    public double CloudCoveragePercent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}