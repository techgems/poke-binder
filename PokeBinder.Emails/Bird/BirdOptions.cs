using System.Text.RegularExpressions;

namespace PokeBinder.Emails.Bird;

/// <summary>
/// The <c>Bird</c> configuration section. Locally the AppHost fills it in from its user secrets;
/// in production it comes from the environment (<c>Bird__ApiKey</c>, <c>Bird__FromMail</c>,
/// <c>Bird__FromName</c>).
/// </summary>
public sealed partial class BirdOptions
{
    public const string Section = "Bird";

    /// <summary>
    /// A workspace API key, <c>bk_{region}_…</c>. The region in it decides which regional API the
    /// email is sent through, so there is no separate setting for that to disagree with.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The From address. Its domain must be verified in the Bird workspace the key belongs to.
    /// Optional at startup, because the sending domain can be set up after the key; a send
    /// without it fails.
    /// </summary>
    public string? FromMail { get; set; }

    /// <summary>The From display name, or nothing for the bare address.</summary>
    public string? FromName { get; set; }

    /// <summary>The region the key belongs to (<c>us1</c>, <c>eu1</c>), or null for a key that names none.</summary>
    public string? Region => KeyPrefix().Match(ApiKey) is { Success: true } match ? match.Groups["region"].Value : null;

    /// <summary>The regional API the key belongs to.</summary>
    public Uri BaseAddress => new($"https://{Region}.platform.bird.com/");

    // Workspace keys are bk_, organisation master keys bm_; both carry the region next.
    [GeneratedRegex("^b[km]_(?<region>[a-z0-9]+)_")]
    private static partial Regex KeyPrefix();
}
