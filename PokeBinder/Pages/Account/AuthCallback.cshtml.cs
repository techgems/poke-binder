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

    public async Task<IActionResult> OnGet(string? returnUrl = null)
    {
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

            //Successful login page
            return LocalRedirect(ReturnUrl ?? "/Protected");
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