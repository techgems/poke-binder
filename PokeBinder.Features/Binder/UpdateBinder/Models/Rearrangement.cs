using System.Text.Json.Serialization;

namespace PokeBinder.Features.Binder.UpdateBinder.Models;

/// <summary>
/// What a binder edit does with the cards already placed in it. The workspace only asks when the
/// new size would move them; an edit that leaves them where they are carries none.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Rearrangement>))]
public enum Rearrangement
{
    /// <summary>Same order as now, laid out from the first pocket: empty pockets and blank pages close up.</summary>
    Keep,

    /// <summary>Sorted by the request's criteria, laid out from the first pocket.</summary>
    Sort,

    /// <summary>Every placed card goes back to the tray, and the binder starts empty.</summary>
    Tray,

    /// <summary>Every placed card is removed, and the binder starts empty.</summary>
    Drop,
}
