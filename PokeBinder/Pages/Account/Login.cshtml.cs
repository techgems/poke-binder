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

    private async Task GenerateTokenAndLink(User user, string email)
    {
        var token = await _userManager.GenerateUserTokenAsync(user, PasswordlessConstants.ProviderName, "passwordless-auth");

        var url = Url.Page("AuthCallback", "Account", new { token, email = Input.Email }, Request.Scheme);

        System.IO.File.WriteAllText("passwordless.txt", url);
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

    }
}
