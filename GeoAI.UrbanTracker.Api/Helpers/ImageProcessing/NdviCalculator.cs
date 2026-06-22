namespace GeoAI.UrbanTracker.Api.Helpers.ImageProcessing;

public static class NdviCalculator
{
    // NDVI = (NIR - Red) / (NIR + Red)
    // Sentinel-2 true color PNG: R=Red(B04), G=Green(B03), B=Blue(B02)
    // NIR не доступний у true color PNG, тому використовуємо Red як проксі
    // Для точного NDVI потрібен окремий запит з NIR каналом
    // Цей метод дає відносну оцінку "зелені" через співвідношення каналів
    public static double EstimateVegetationIndex(byte r, byte g, byte b)
    {
        // Vegetation proxy: зелені пікселі мають g >> r і g >> b
        var total = r + g + b;
        if (total == 0) return 0;
        return (g - Math.Max(r, b)) / (double)total;
    }

    public static bool IsBuiltUp(byte r, byte g, byte b)
    {
        // Забудова: сірі/білі тони (r ≈ g ≈ b, всі відносно високі)
        var diff = Math.Max(Math.Abs(r - g), Math.Max(Math.Abs(r - b), Math.Abs(g - b)));
        var brightness = (r + g + b) / 3.0;
        return diff < 20 && brightness > 80;
    }
}