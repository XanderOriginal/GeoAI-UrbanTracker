namespace GeoAI.UrbanTracker.Api.Models;

public class AnalysisResult
{
    public Guid Id { get; set; }

    public Guid AnalysisRequestId { get; set; }
    public AnalysisRequest AnalysisRequest { get; set; } = null!;

    // Результати ImageDiffService
    public double NdviBefore { get; set; }
    public double NdviAfter { get; set; }
    public double NdviChangePercent { get; set; }

    public double BuiltUpAreaChangePercent { get; set; }
    public double GreenAreaChangePercent { get; set; }

    // Текстовий висновок від Gemini
    public string GeminiSummary { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}