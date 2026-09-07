using System.Text.Json;
using TechGems.StaticComponents;

namespace PokeBinder.Components;

/// <summary>
/// Hands a server-side object to the SPA as part of the page, so the app has its data before it has
/// made a single request.
/// <para>
/// The object is written into one namespace on <c>window</c> — <c>window.pokeBinder</c> — under the
/// key given here. It has to be reachable from a bundle this page knows nothing about, and a module
/// cannot import from a Razor view, so some agreed global is the only channel; putting every
/// payload under one object keeps that to a single name instead of one per page.
/// </para>
/// </summary>
public class ServerPreload : StaticComponent
{
    /// <summary>
    /// How the payload is written. Web defaults on purpose: this produces exactly what the same
    /// object would look like coming back from an API endpoint, so the client reads one shape
    /// whether the data was preloaded or fetched, and one TypeScript type describes both.
    /// <para>
    /// The default encoder is what makes this safe to drop inside a &lt;script&gt; block: it escapes
    /// &lt;, &gt; and &amp; as < and friends, so a binder named "&lt;/script&gt;" cannot close
    /// the tag it is being written into.
    /// </para>
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The name the payload takes inside the namespace, e.g. "binder" for
    /// <c>window.pokeBinder.binder</c>.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The object to hand over. Null writes null, which the client reads as "not preloaded".</summary>
    public object? Data { get; set; }

    /// <summary>The payload as a JavaScript object literal. JSON is one, so this needs no parsing.</summary>
    public string DataLiteral => JsonSerializer.Serialize(Data, SerializerOptions);

    /// <summary>The key as a quoted JavaScript string, escaped the same way.</summary>
    public string KeyLiteral => JsonSerializer.Serialize(Key, SerializerOptions);
}
