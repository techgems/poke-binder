using System.Text.Json.Serialization;

namespace PokeBinder.Features.Binder.AddSetToBinder.Models;

/// <summary>
/// Where a set goes relative to the cards already in the binder. A buffer page and sending them to
/// the end are one or the other, never both, so they are one choice with a third answer for
/// neither. Ignored when the binder is emptied first or holds nothing.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ExistingCards>))]
public enum ExistingCards
{
    /// <summary>The set starts on the first page after the last one holding a card.</summary>
    After,

    /// <summary>One blank page after the last card, and the set after that.</summary>
    Buffer,

    /// <summary>
    /// The set takes the front of the binder, and the cards already there move back after it by
    /// whole pages, each keeping its place on its page.
    /// </summary>
    End,
}
