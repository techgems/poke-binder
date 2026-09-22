using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Binders.Users.Totp;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace PokeBinder.Pages.Account;

public class AuthCallbackModel : PageModel
{
    private readonly UserManager<User> _userManager;

    public AuthCallbackModel(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Where a signed-in user lands. <c>/my-binders</c> is the shelf, which is the one page that
    /// makes sense with no binder chosen yet -- <c>/Binder</c> without an id renders the 404 page.
    /// </summary>
    private const string DefaultDestination = "/my-binders";

    public async Task<IActionResult> OnGet(string? returnUrl = null)
    {
        // Where the user was going before they were asked to sign in, carried here by the link the
        // login page built. Checked again rather than trusted: the link is a URL in somebody's
        // inbox, and LocalRedirect answers a non-local one with an exception -- which would be a
        // 500 at the end of a sign-in that had otherwise worked. Falling back to the default makes
        // a tampered link an ordinary sign-in instead.
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        var user = await _userManager.FindByEmailAsync(Email);
        var isValid = await _userManager.VerifyUserTokenAsync(user, PasswordlessConstants.ProviderName, "passwordless-auth", Token);

        if (isValid)
        {
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            await _userManager.UpdateSecurityStampAsync(user);

            var claimsIdentity = new ClaimsIdentity(new List<Claim> { new Claim("sub", user.Id.ToString()) }, IdentityConstants.ApplicationScheme);

            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(claimsIdentity));

            // LocalRedirect, not Redirect: a returnUrl off the query string is caller-supplied, and
            // this refuses an absolute one rather than forwarding somebody off the site.
            return LocalRedirect(ReturnUrl ?? DefaultDestination);
        }

        //Failed due to wrong token or expired token. Could show a page about the token being invalid or expired and a button to resend the email.
        return LocalRedirect("/Account/Login");
    }

    [BindProperty(SupportsGet = true)]
    [Required]
    public string Email { get; set; }

    [BindProperty(SupportsGet = true)]
    [Required]
    public string Token { get; set; }
}