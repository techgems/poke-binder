using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Binders.DbContext.Entities;

namespace PokeBinder.Features.Binder.CreateBinderCardLabel;

/// <summary>
/// Creates one label on a binder. The one dedicated label slice: a label has a real id before
/// anything else can reference it, so the Create-and-apply flow in the pocket's Label modal calls
/// this first and lands the real id in the next debounced <c>SaveBinderChanges</c>. Rename, recolor
/// and delete are not here -- they sync through <c>UpdateBinder</c>'s labels field, which is a
/// settings-tab Save-button operation rather than a per-action write.
///
/// <para>
/// The handler is the happy path only. <see cref="CreateBinderCardLabelValidator"/> runs first and
/// rejects everything that could fail here: a nameless label, an unknown colour, a duplicate name
/// in this binder, and someone else's binder.
/// </para>
/// </summary>
public static class CreateBinderCardLabel
{
    /// <summary>
    /// The longest a tag can be. The chip has to fit the pocket's bottom-left corner at a small
    /// grid size, and the client clamps the input to the same number.
    /// </summary>
    public const int MaxNameLength = 20;

    /// <summary>
    /// The eight colours the collector can pick from. Stored as names rather than hex so the
    /// swatches can be restyled without rewriting any row. Kept in sync with
    /// <c>LABEL_COLORS</c> in <c>stores/labels.svelte.ts</c>.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedColors = new HashSet<string>(StringComparer.Ordinal)
    {
        "red", "orange", "amber", "green", "teal", "blue", "purple", "pink",
    };

    /// <summary>
    /// A new label as the user filled it in. The binder is in the URL; the field on the request is
    /// overwritten by the route before anything reads it, so a request body that names a different
    /// binder is ignored.
    /// </summary>
    public record Request
    {
        public int BinderId { get; init; }

        /// <summary>
        /// The tag. Trimmed by the handler; the validator refuses a blank one, a name past
        /// <see cref="MaxNameLength"/>, and one already taken on this binder.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// One of <see cref="AllowedColors"/>. The validator refuses anything else and the client
        /// only sends from that list.
        /// </summary>
        public string? Color { get; init; }
    }

    /// <summary>
    /// The server-assigned id. The client uses it to swap the local negative id in its
    /// pocket-assignments map before the next <c>SaveBinderChanges</c> fires.
    /// </summary>
    public record Response(int Id);

    /// <param name="userId">The signed-in user. The validator has already established that the
    /// binder is theirs; the handler does not re-check.</param>
    public static async Task<Response> Handler(
        Request request,
        int userId,
        BinderDbContext context,
        CancellationToken ct = default)
    {
        // Trimming is tidying, not validating: the validator already refused a name that was
        // nothing but spaces and one past the cap.
        var name = request.Name!.Trim();

        var label = new BinderCardLabel
        {
            BinderId = request.BinderId,
            Name = name,
            Color = request.Color!,
        };

        context.BinderCardLabels.Add(label);

        await context.SaveChangesAsync(ct);

        return new Response(label.Id);
    }
}
