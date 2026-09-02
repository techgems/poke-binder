namespace PokeBinder.Features.CardImages;

/// <summary>
/// Turns the image path the ETL recorded on a card into something a browser can load.
/// <para>
/// Card art lives on the machine that ran the ETL, stored as an absolute path such as
/// <c>C:\ImagesTCG\Base Set\42382_in_1000x1000.jpg</c>. That is a file path, not a URL, so the
/// stored root is swapped for the configured public base. Moving the art to a CDN is then a matter
/// of pointing the base at the CDN — nothing here or in the query has to change.
/// </para>
/// </summary>
public sealed class CardImageUrls(string localRoot, string publicBaseUrl)
{
    /// <summary>The stored prefix that gets replaced, with any trailing separator removed.</summary>
    private string LocalRoot { get; } = localRoot.Replace('\\', '/').TrimEnd('/');

    private string PublicBaseUrl { get; } = publicBaseUrl.TrimEnd('/');

    /// <summary>
    /// The public URL for a stored path, or null when there is no art or it sits outside
    /// <see cref="LocalRoot"/> — a path we cannot serve is better dropped than handed to the
    /// browser as a broken image.
    /// </summary>
    public string? ToPublicUrl(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        var path = storedPath.Replace('\\', '/');

        if (!path.StartsWith($"{LocalRoot}/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relativePath = path[(LocalRoot.Length + 1)..];

        // Set folders carry spaces, ampersands and apostrophes, so each segment is escaped on its
        // own; escaping the whole path at once would swallow the separators too.
        var escapedPath = string.Join('/', relativePath.Split('/').Select(Uri.EscapeDataString));

        return $"{PublicBaseUrl}/{escapedPath}";
    }
}
