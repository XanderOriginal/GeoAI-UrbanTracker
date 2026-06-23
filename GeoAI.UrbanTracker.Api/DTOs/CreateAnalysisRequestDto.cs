using System.ComponentModel.DataAnnotations;

namespace GeoAI.UrbanTracker.Api.DTOs;

public class CreateAnalysisRequestDto
{
    [Required]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Required]
    [Range(100, 50000)]
    public int RadiusMeters { get; set; } = 1000;

    [Required]
    public DateOnly DateFrom { get; set; }

    [Required]
    public DateOnly DateTo { get; set; }
}