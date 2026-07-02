namespace GeoAI.UrbanTracker.Api.Helpers.ImageProcessing;

public static class ImageSourceLoader
{
    public static async Task<byte[]> LoadBytesAsync(
        HttpClient httpClient,
        string pathOrUrl,
        CancellationToken cancellationToken = default)
    {
        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return await httpClient.GetByteArrayAsync(uri, cancellationToken);
        }

        return await File.ReadAllBytesAsync(pathOrUrl, cancellationToken);
    }
}