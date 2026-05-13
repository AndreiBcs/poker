using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Source.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    public LoginInput Login { get; set; } = new();

    [BindProperty]
    public SignupInput Signup { get; set; } = new();

    public IActionResult OnPostLogin()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        TempData["StatusMessage"] = "Signed in (demo).";
        return RedirectToPage("/Lobby/Index");
    }

    public IActionResult OnPostSignup()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        TempData["StatusMessage"] = "Account created (demo).";
        return RedirectToPage("/Lobby/Index");
    }

    public class LoginInput
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class SignupInput
    {
        [Required, StringLength(32, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(64, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
    }
}
