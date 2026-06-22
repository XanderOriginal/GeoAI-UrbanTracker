using GeoAI.UrbanTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GeoAI.UrbanTracker.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AnalysisRequest> AnalysisRequests => Set<AnalysisRequest>();
    public DbSet<SatelliteImage> SatelliteImages => Set<SatelliteImage>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AnalysisRequest -> SatelliteImages (один до багатьох)
        modelBuilder.Entity<AnalysisRequest>()
            .HasMany(ar => ar.SatelliteImages)
            .WithOne(si => si.AnalysisRequest)
            .HasForeignKey(si => si.AnalysisRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // AnalysisRequest -> AnalysisResult (один до одного)
        modelBuilder.Entity<AnalysisRequest>()
            .HasOne(ar => ar.Result)
            .WithOne(r => r.AnalysisRequest)
            .HasForeignKey<AnalysisResult>(r => r.AnalysisRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enum зберігаємо як рядок, а не число — зручніше дивитись в БД напряму
        modelBuilder.Entity<AnalysisRequest>()
            .Property(ar => ar.Status)
            .HasConversion<string>();
    }
}