using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Binders.Users.Totp;
using PokeBinder.Emails;
using PokeBinder.Emails.Messages;
using System.ComponentModel.DataAnnotations;

namespace PokeBinder.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly Mailer _mailer;
    private readonly TimeSpan _linkLifespan;
    private readonly bool _writeLinkToFile;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        Mailer mailer,
        IOptions<PasswordlessLoginTokenProviderOptions> tokenOptions,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _mailer = mailer;
        _linkLifespan = tokenOptions.Value.TokenLifespan;
        // Program.cs already refuses the setting outside Development; the check here keeps this
        // page from honouring it even if that ever changed.
        _writeLinkToFile = environment.IsDevelopment()
            && configuration.GetValue<bool>("Passwordless:WriteLinkToFile");
        _logger = logger;
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

            await SendSignInLink(user, Input.Email, HttpContext.RequestAborted);

            return Page();
        }

        //Ditch this implementation in favor of this one: https://andrewlock.net/implementing-custom-token-providers-for-passwordless-authentication-in-asp-net-core-identity/
        await SendSignInLink(user, Input.Email, HttpContext.RequestAborted);

        //Could keep in the same page instead and show a message about the email being sent.
        return Page();
    }

    /// <summary>
    /// Builds the one-time link and mails it through Bird as the <c>sign-in</c> email -- or, in
    /// Development with <c>Passwordless:WriteLinkToFile</c> on, writes it to <c>passwordless.txt</c>
    /// instead, for signing in without a mail provider.
    ///
    /// <para>
    /// The link carries where the user was going as well as who they are. It has to: the sign-in
    /// leaves the browser and comes back as a fresh GET on the callback, so anything this page knew
    /// and did not put in the link is gone by the time they are signed in. Without it every
    /// sign-in lands on the default page, however the user got here.
    /// </para>
    /// </summary>
    private async Task SendSignInLink(User user, string email, CancellationToken ct)
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

        if (_writeLinkToFile)
        {
            await System.IO.File.WriteAllTextAsync("passwordless.txt", url, ct);
            ShowTokenSentMessage = true;
            return;
        }

        try
        {
            await _mailer.SendAsync(email, new SignInEmail(email, url!, (int)_linkLifespan.TotalMinutes), ct);
        }
        catch (EmailException ex)
        {
            // The account (if this was a sign-up) exists either way; another attempt mails a new
            // link. What went wrong is for the log, not for the person signing in.
            _logger.LogError(ex, "The sign-in email to {Email} was not sent.", email);
            ModelState.AddModelError(string.Empty, "We couldn't send your sign-in email. Please try again in a moment.");
            return;
        }

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
