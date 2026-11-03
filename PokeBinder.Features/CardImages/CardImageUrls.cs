namespace PokeBinder.Features.CardImages;

/// <summary>
/// Turns the image key stored on a card into something a browser can load.
/// <para>
/// A card stores the object key of its art in the card-image bucket, such as
/// <c>Base Set/42382_in_1000x1000.jpg</c>, and the bucket is served from behind a CDN. The URL is
/// the CDN's base with the key appended, so where the art is served from is configuration alone --
/// nothing here or in the query knows which CDN, or whether there is one.
/// </para>
/// </summary>
public sealed class CardImageUrls(string publicBaseUrl)
{
    private string PublicBaseUrl { get; } = publicBaseUrl.TrimEnd('/');

    /// <summary>The public URL for a stored key, or null when the card has no art.</summary>
    public string? ToPublicUrl(string? objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return null;
        }

        // Set folders carry spaces, ampersands and apostrophes, so each segment is escaped on its
        // own; escaping the whole key at once would swallow the separators too.
        var escapedKey = string.Join('/', objectKey.TrimStart('/').Split('/').Select(Uri.EscapeDataString));

        return $"{PublicBaseUrl}/{escapedKey}";
    }
}
