using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace PokeBinder.Emails.Bird;

/// <summary>
/// Sends through Bird's email API: <c>POST /v1/email/messages</c> on the key's regional host,
/// with the key as a bearer token. The HttpClient arrives with both already set; see
/// <c>AddBirdEmailSender</c>.
/// </summary>
internal sealed class BirdEmailSender(HttpClient http, IOptions<BirdOptions> options) : IEmailSender
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task SendAsync(string to, RenderedEmail email, CancellationToken ct)
    {
        var bird = options.Value;
        if (string.IsNullOrWhiteSpace(bird.FromMail))
            throw new EmailSendException($"Bird:FromMail is not set, so the email to {to} was not sent.");

        var message = new Message(
            From: new Address(bird.FromMail, string.IsNullOrWhiteSpace(bird.FromName) ? null : bird.FromName),
            To: [new Address(to, null)],
            Subject: email.Subject,
            Html: email.Html,
            Text: email.Text,
            // Every email the app sends answers something the user did. Marketing, Bird's default
            // for an inline send, would hold it to the marketing suppression list.
            Category: "transactional",
            // Click tracking rewrites every link through Bird's redirector, which would put a
            // sign-in link's one-time token through a third party. Opens are not worth a pixel.
            TrackOpens: false,
            TrackClicks: false);

        using var response = await http.PostAsJsonAsync("v1/email/messages", message, Json, ct);
        if (response.IsSuccessStatusCode) return;

        // Bird's error body says which field it refused, or that the From domain is unverified.
        var detail = await response.Content.ReadAsStringAsync(ct);
        throw new EmailSendException(
            $"Bird refused the email to {to} with {(int)response.StatusCode} {response.ReasonPhrase}: {detail}");
    }

    private sealed record Message(
        Address From,
        IReadOnlyList<Address> To,
        string Subject,
        string Html,
        string? Text,
        string Category,
        bool TrackOpens,
        bool TrackClicks);

    private sealed record Address(string Email, string? Name);
}
