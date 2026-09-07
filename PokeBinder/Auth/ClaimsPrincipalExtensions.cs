using System.Security.Claims;

namespace PokeBinder.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The signed-in user's id.
    /// <para>
    /// AuthCallback signs people in with a hand-built identity carrying a single "sub" claim rather
    /// than ClaimTypes.NameIdentifier, so this reads that -- and UserManager.GetUserId, which looks
    /// for the latter, returns null here. Kept in one place so that whenever the sign-in starts
    /// issuing the standard claim, one file changes.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The principal carries no usable id. [Authorize] guarantees an identity, not this particular
    /// claim, and every caller scopes a query by the id it gets back -- so a missing one has to
    /// stop the request rather than default to zero and show one user another user's shelf.
    /// </exception>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue("sub");

        return int.TryParse(sub, out var userId)
            ? userId
            : throw new InvalidOperationException("The signed-in principal carries no usable \"sub\" claim.");
    }
}
