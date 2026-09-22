using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Binders.Users.Totp;
using System.ComponentModel.DataAnnotations;

namespace PokeBinder.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public LoginModel(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public bool ShowTokenSentMessage { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email);

        //Enter register flow instead.
        if (user == null)
        {
            user = new User
            {
                Email = Input.Email
            };

            var registerResult = await _userManager.CreateAsync(user);

            if (!registerResult.Succeeded)
            {
                foreach (var error in registerResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            await GenerateTokenAndLink(user, Input.Email);

            return Page();
        }

        //Ditch this implementation in favor of this one: https://andrewlock.net/implementing-custom-token-providers-for-passwordless-authentication-in-asp-net-core-identity/
        await GenerateTokenAndLink(user, Input.Email);

        //Could keep in the same page instead and show a message about the email being sent.
        return Page();
    }

    /// <summary>
    /// Builds the one-time link and, in Development, writes it to <c>passwordless.txt</c> instead of
    /// mailing it.
    ///
    /// <para>
    /// The link carries where the user was going as well as who they are. It has to: the sign-in
    /// leaves the browser and comes back as a fresh GET on the callback, so anything this page knew
    /// and did not put in the link is gone by the time they are signed in. Without it every
    /// sign-in lands on the default page, however the user got here.
    /// </para>
    /// </summary>
    private async Task GenerateTokenAndLink(User user, string email)
    {
        var token = await _userManager.GenerateUserTokenAsync(user, PasswordlessConstants.ProviderName, "passwordless-auth");

        // Only a local destination travels. It arrives on this page's own query string, so anybody
        // can put anything there -- and a link that carried an absolute URL would either walk the
        // user off the site or, since the callback redirects locally, throw at the very end of a
        // sign-in that had already happened. Dropped here, a crafted returnUrl is just an ordinary
        // sign-in. Url.Page leaves the parameter out of the link entirely when it is null.
        var destination = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;

        // `email` rather than `Input.Email`: the same value at both call sites, but a parameter the
        // method ignored was one waiting to disagree with the user it was handed.
        var url = Url.Page(
            "AuthCallback",
            "Account",
            new { token, email, returnUrl = destination },
            Request.Scheme);

        System.IO.File.WriteAllText("passwordless.txt", url);

        ShowTokenSentMessage = true;
    }

    public class InputModel
    {
        [Display(Name = "Email")]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

    }
}
