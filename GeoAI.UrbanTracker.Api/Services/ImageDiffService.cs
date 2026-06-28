using GeoAI.UrbanTracker.Api.Helpers.ImageProcessing;
using SkiaSharp;

namespace GeoAI.UrbanTracker.Api.Services;

public class ImageDiffService : IImageDiffService
{
    private readonly ILogger<ImageDiffService> _logger;

    public ImageDiffService(ILogger<ImageDiffService> logger)
    {
        _logger = logger;
    }

    public async Task<ImageDiffResult> ComputeDiffAsync(
        string beforeImagePath,
        string afterImagePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Computing spectral diff: {Before} → {After}",
            beforeImagePath, afterImagePath);

        var beforeBytes = await File.ReadAllBytesAsync(beforeImagePath, cancellationToken);
        var afterBytes = await File.ReadAllBytesAsync(afterImagePath, cancellationToken);

        var before = AnalyzeImage(beforeBytes, "BEFORE");
        var after = AnalyzeImage(afterBytes, "AFTER");

        // Якщо один зі знімків порожній — аналіз безглуздий, кидаємо виняток.
        // SatelliteImageService має відфільтрувати такі знімки за розміром,
        // але цей guard — останній рубіж захисту.
        if (before.ValidPixels == 0)
            throw new InvalidOperationException(
                "BEFORE image has no valid pixels (cloud-covered or corrupted). " +
                "Try a different date range.");
        if (after.ValidPixels == 0)
            throw new InvalidOperationException(
                "AFTER image has no valid pixels (cloud-covered or corrupted). " +
                "Try a different date range.");

        _logger.LogInformation(
            "BEFORE → exgMean={V:F4} builtUp={BU:P1} green={G:P1} water={W:P1} bare={Ba:P1} " +
            "vegStdDev={SD:F4} validPx={VP}",
            before.VegIndexMean, before.BuiltUpRatio, before.GreenRatio,
            before.WaterRatio, before.BareRatio, before.VegIndexStdDev, before.ValidPixels);

        _logger.LogInformation(
            "AFTER  → exgMean={V:F4} builtUp={BU:P1} green={G:P1} water={W:P1} bare={Ba:P1} " +
            "vegStdDev={SD:F4} validPx={VP}",
            after.VegIndexMean, after.BuiltUpRatio, after.GreenRatio,
            after.WaterRatio, after.BareRatio, after.VegIndexStdDev, after.ValidPixels);

        // ── Statistical significance check (Welch's t-test proxy) ────────────
        // Tells us if the vegetation change is real or just noise.
        // t = (μ_after − μ_before) / sqrt(σ²_before/N_before + σ²_after/N_after)
        double vegTStat = ComputeTStatistic(
            before.VegIndexMean, before.VegIndexStdDev, before.ValidPixels,
            after.VegIndexMean, after.VegIndexStdDev, after.ValidPixels);

        _logger.LogInformation(
            "Welch t-statistic for vegetation change: {T:F3} " +
            "(|t|>2 → statistically significant at p<0.05)",
            vegTStat);

        // All deltas in percentage points
        double builtUpChangePp = (after.BuiltUpRatio - before.BuiltUpRatio) * 100.0;
        double greenChangePp = (after.GreenRatio - before.GreenRatio) * 100.0;
        double ndviChangePp = (after.VegIndexMean - before.VegIndexMean) * 100.0;

        _logger.LogInformation(
            "DIFF → builtUp={BU:+0.00;-0.00}pp  green={G:+0.00;-0.00}pp  ndvi={N:+0.00;-0.00}pp",
            builtUpChangePp, greenChangePp, ndviChangePp);

        return new ImageDiffResult(
            NdviBefore: before.VegIndexMean,
            NdviAfter: after.VegIndexMean,
            NdviChangePercent: ndviChangePp,
            BuiltUpAreaChangePercent: builtUpChangePp,
            GreenAreaChangePercent: greenChangePp);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Per-image spectral analysis with running variance (Welford's algorithm)
    // ─────────────────────────────────────────────────────────────────────────

    private SpectralProfile AnalyzeImage(byte[] imageBytes, string label)
    {
        using var bitmap = SKBitmap.Decode(imageBytes)
            ?? throw new InvalidOperationException($"Cannot decode {label} image.");

        // Sample every pixel up to 512×512; stride 2 for larger images
        int stepX = bitmap.Width > 1024 ? 2 : 1;
        int stepY = bitmap.Height > 1024 ? 2 : 1;

        long validPixels = 0;
        long greenPixels = 0;
        long builtUpPixels = 0;
        long waterPixels = 0;
        long barePixels = 0;

        // Welford's online algorithm for mean + variance in one pass
        // (avoids storing all pixel values)
        double welfMean = 0.0;
        double welfM2 = 0.0;  // sum of squared deviations

        for (int y = 0; y < bitmap.Height; y += stepY)
            for (int x = 0; x < bitmap.Width; x += stepX)
            {
                var px = bitmap.GetPixel(x, y);
                byte r = px.Red, g = px.Green, b = px.Blue;

                int brightness = r + g + b;
                if (brightness < 15) continue; // no-data black
                if (r > 245 && g > 245 && b > 245) continue; // cloud white

                validPixels++;

                double vegIdx = NdviCalculator.EstimateVegetationIndex(r, g, b);

                // Welford's update
                double delta = vegIdx - welfMean;
                welfMean += delta / validPixels;
                double delta2 = vegIdx - welfMean;
                welfM2 += delta * delta2;

                if (NdviCalculator.IsVegetated(r, g, b)) greenPixels++;
                if (NdviCalculator.IsBuiltUp(r, g, b)) builtUpPixels++;
                if (NdviCalculator.IsWater(r, g, b)) waterPixels++;
                if (NdviCalculator.IsBareOrArid(r, g, b)) barePixels++;
            }

        long totalSampled = (long)(bitmap.Width / stepX) * (bitmap.Height / stepY);
        if (validPixels < totalSampled * 0.10)
        {
            _logger.LogWarning("{Label}: <10% valid pixels — likely cloud/no-data image", label);
            return SpectralProfile.Empty;
        }

        double variance = validPixels > 1 ? welfM2 / (validPixels - 1) : 0.0;
        double stdDev = Math.Sqrt(variance);

        return new SpectralProfile
        {
            ValidPixels = validPixels,
            VegIndexMean = welfMean,
            VegIndexStdDev = stdDev,
            GreenRatio = greenPixels / (double)validPixels,
            BuiltUpRatio = builtUpPixels / (double)validPixels,
            WaterRatio = waterPixels / (double)validPixels,
            BareRatio = barePixels / (double)validPixels,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Welch's t-statistic (unequal variance t-test)
    // t = (μ₁ − μ₂) / sqrt(σ₁²/n₁ + σ₂²/n₂)
    // |t| > 2 → significant at p < 0.05 for large n
    // ─────────────────────────────────────────────────────────────────────────
    private static double ComputeTStatistic(
        double mean1, double std1, long n1,
        double mean2, double std2, long n2)
    {
        double se = Math.Sqrt(std1 * std1 / n1 + std2 * std2 / n2);
        return se < 1e-12 ? 0.0 : (mean2 - mean1) / se;
    }

    private record SpectralProfile
    {
        public long ValidPixels { get; init; }
        public double VegIndexMean { get; init; }
        public double VegIndexStdDev { get; init; }
        public double GreenRatio { get; init; }
        public double BuiltUpRatio { get; init; }
        public double WaterRatio { get; init; }
        public double BareRatio { get; init; }

        public static SpectralProfile Empty => new();
    }
}