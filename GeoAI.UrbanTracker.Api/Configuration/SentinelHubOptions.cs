namespace GeoAI.UrbanTracker.Api.Configuration;

public class SentinelHubOptions
{
    public const string SectionName = "SentinelHub";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}