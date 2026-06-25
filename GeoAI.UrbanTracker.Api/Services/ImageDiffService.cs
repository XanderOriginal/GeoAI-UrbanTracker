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
        _logger.LogInformation("Computing image diff between {Before} and {After}",
            beforeImagePath, afterImagePath);

        var beforeBytes = await File.ReadAllBytesAsync(beforeImagePath, cancellationToken);
        var afterBytes = await File.ReadAllBytesAsync(afterImagePath, cancellationToken);

        var beforeStats = AnalyzeImage(beforeBytes);
        var afterStats = AnalyzeImage(afterBytes);

        var ndviChangePct = beforeStats.avgVegetation > 0
            ? ((afterStats.avgVegetation - beforeStats.avgVegetation) / beforeStats.avgVegetation) * 100
            : 0;

        var builtUpChangePct = beforeStats.builtUpRatio > 0
            ? ((afterStats.builtUpRatio - beforeStats.builtUpRatio) / beforeStats.builtUpRatio) * 100
            : afterStats.builtUpRatio * 100;

        var greenChangePct = beforeStats.greenRatio > 0
            ? ((afterStats.greenRatio - beforeStats.greenRatio) / beforeStats.greenRatio) * 100
            : 0;

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
    byte[] imageBytes)
    {
        using var bitmap = SKBitmap.Decode(imageBytes);

        double totalVegetation = 0;
        int builtUpPixels = 0;
        int greenPixels = 0;
        int validPixels = 0; // рахуємо тільки не-чорні пікселі

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);

                // Пропускаємо повністю чорні пікселі (no-data)
                if (pixel.Red < 5 && pixel.Green < 5 && pixel.Blue < 5)
                    continue;

                validPixels++;
                totalVegetation += NdviCalculator.EstimateVegetationIndex(
                    pixel.Red, pixel.Green, pixel.Blue);

                if (NdviCalculator.IsBuiltUp(pixel.Red, pixel.Green, pixel.Blue))
                    builtUpPixels++;

                // Зелений: g помітно більший за r і b
                if (pixel.Green > pixel.Red + 15 && pixel.Green > pixel.Blue + 15)
                    greenPixels++;
            }
        }

        // Якщо занадто мало валідних пікселів — знімок поганий
        int total = bitmap.Width * bitmap.Height;
        if (validPixels < total * 0.1) // менше 10% валідних пікселів
        {
            return (0, 0, 0);
        }

        return (
            avgVegetation: totalVegetation / validPixels,
            builtUpRatio: builtUpPixels / (double)validPixels,
            greenRatio: greenPixels / (double)validPixels
        );
    }
}