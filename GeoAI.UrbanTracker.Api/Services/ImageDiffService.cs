using GeoAI.UrbanTracker.Api.Helpers.ImageProcessing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
        _logger.LogInformation("Computing image diff between {Before} and {After}",
            beforeImagePath, afterImagePath);

        using var beforeImage = await Image.LoadAsync<Rgb24>(beforeImagePath, cancellationToken);
        using var afterImage = await Image.LoadAsync<Rgb24>(afterImagePath, cancellationToken);

        var beforeStats = AnalyzeImage(beforeImage);
        var afterStats = AnalyzeImage(afterImage);

        var ndviChangePct = beforeStats.avgVegetation > 0
            ? ((afterStats.avgVegetation - beforeStats.avgVegetation) / beforeStats.avgVegetation) * 100
            : afterStats.avgVegetation * 100;

        var builtUpChangePct = beforeStats.builtUpRatio > 0
            ? ((afterStats.builtUpRatio - beforeStats.builtUpRatio) / beforeStats.builtUpRatio) * 100
            : afterStats.builtUpRatio * 100;

        var greenChangePct = beforeStats.greenRatio > 0
            ? ((afterStats.greenRatio - beforeStats.greenRatio) / beforeStats.greenRatio) * 100
            : afterStats.greenRatio * 100;

        _logger.LogInformation(
            "Diff computed: NDVI {NdviChange:+0.00;-0.00}%, BuiltUp {BuiltUpChange:+0.00;-0.00}%",
            ndviChangePct, builtUpChangePct);

        return new ImageDiffResult(
            NdviBefore: beforeStats.avgVegetation,
            NdviAfter: afterStats.avgVegetation,
            NdviChangePercent: ndviChangePct,
            BuiltUpAreaChangePercent: builtUpChangePct,
            GreenAreaChangePercent: greenChangePct);
    }

    private static (double avgVegetation, double builtUpRatio, double greenRatio) AnalyzeImage(
        Image<Rgb24> image)
    {
        double totalVegetation = 0;
        int builtUpPixels = 0;
        int greenPixels = 0;
        int totalPixels = image.Width * image.Height;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    totalVegetation += NdviCalculator.EstimateVegetationIndex(pixel.R, pixel.G, pixel.B);

                    if (NdviCalculator.IsBuiltUp(pixel.R, pixel.G, pixel.B))
                        builtUpPixels++;

                    // Зелений піксель: g значно більший за r і b
                    if (pixel.G > pixel.R + 10 && pixel.G > pixel.B + 10)
                        greenPixels++;
                }
            }
        });

        return (
            avgVegetation: totalVegetation / totalPixels,
            builtUpRatio: builtUpPixels / (double)totalPixels,
            greenRatio: greenPixels / (double)totalPixels
        );
    }
}