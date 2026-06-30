using SkiaSharp;

namespace GeoAI.UrbanTracker.Api.Helpers.ImageProcessing;

/// <summary>
/// Декодування PNG зі знімків Sentinel Hub.
///
/// ВАЖЛИВО: наші evalscript-и кладуть у 4-й (alpha) канал не реальну прозорість,
/// а службовий прапор isBadPixel (255 = хмара/тінь/no-data, 0 = валідний піксель).
/// SKBitmap.Decode за замовчуванням повертає зображення з PREMULTIPLIED alpha —
/// тобто кожен R/G/B-канал множиться на alpha/255 під час декодування. Оскільки
/// у валідних пікселів alpha = 0, premultiplication перетворює їх R/G/B на 0
/// (чорний), хоча реальні спектральні дані там були нормальні. Це призводить до
/// хибного "<10% valid pixels", навіть коли знімок насправді чистий.
///
/// Рішення — декодувати з SKAlphaType.Unpremul, щоб alpha-канал залишався лише
/// нашим службовим прапором і не псував кольорові дані.
/// </summary>
public static class ImageDecoding
{
    public static SKBitmap? DecodeUnpremultiplied(byte[] pngBytes)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(pngBytes));
        if (codec is null) return null;

        var info = new SKImageInfo(
            codec.Info.Width,
            codec.Info.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Unpremul);

        var bitmap = new SKBitmap(info);
        var result = codec.GetPixels(info, bitmap.GetPixels());

        if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
        {
            bitmap.Dispose();
            return null;
        }

        return bitmap;
    }
}