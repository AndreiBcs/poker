using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Source.Features.User;
using Source.Infrastructure.Authentication;

namespace Source.Pages;

public class IndexModel : PageModel
{
    private readonly UserServices _userServices;
    private readonly UserSessionService _sessionService;

    public IndexModel(UserServices userServices, UserSessionService sessionService)
    {
        _userServices = userServices;
        _sessionService = sessionService;
    }

    [BindProperty]
    public LoginInput Login { get; set; } = new();

    [BindProperty]
    public SignupInput Signup { get; set; } = new();

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToPage("/Lobby/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostLoginAsync()
    {
        if (!TryValidateLogin())
        {
            ViewData["OpenModal"] = "login";
            return Page();
        }

        var user = await _userServices.ValidateUserCredentialsAsync(Login.Email, Login.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            ViewData["OpenModal"] = "login";
            return Page();
        }

        await _sessionService.SignInAsync(HttpContext, user);
        return RedirectToPage("/Lobby/Index");
    }

    public async Task<IActionResult> OnPostSignupAsync()
    {
        if (!TryValidateSignup())
        {
            ViewData["OpenModal"] = "signup";
            return Page();
        }

        var existingUser = await _userServices.GetUserByEmailAsync(Signup.Email);
        if (existingUser is not null)
        {
            ModelState.AddModelError("Signup.Email", "Email is already in use.");
            ViewData["OpenModal"] = "signup";
            return Page();
        }

        var user = await _userServices.CreateUserAsync(new UserCreateRequest(
            Signup.Username,
            Signup.Email,
            Signup.Password));

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Could not create account.");
            ViewData["OpenModal"] = "signup";
            return Page();
        }

        await _sessionService.SignInAsync(HttpContext, user);
        return RedirectToPage("/Lobby/Index");
    }

    private bool TryValidateLogin()
    {
        ModelState.Clear();
        return TryValidateModel(Login, nameof(Login));
    }

    private bool TryValidateSignup()
    {
        ModelState.Clear();
        return TryValidateModel(Signup, nameof(Signup));
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
