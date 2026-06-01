using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Source.Features.Lobby;
using Source.Features.User;
using Source.Infrastructure.Authentication;

namespace Source.Pages.Lobby;

[Authorize]
public class IndexModel : PageModel
{
    private readonly LobbyService _lobbyService;
    private readonly UserServices _userServices;
    private readonly UserSessionService _sessionService;

    public IndexModel(LobbyService lobbyService, UserServices userServices, UserSessionService sessionService)
    {
        _lobbyService = lobbyService;
        _userServices = userServices;
        _sessionService = sessionService;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    public IReadOnlyList<LobbyListItem> Lobbies { get; private set; } = Array.Empty<LobbyListItem>();

    public bool HasPinnedLobby => Lobbies.Any(lobby => lobby.IsPinned);

    public ProfileView Profile { get; private set; } = new();

    [BindProperty]
    public CreateLobbyInput CreateLobby { get; set; } = new();

    [BindProperty]
    public EditLobbyInput EditLobby { get; set; } = new();

    [BindProperty]
    public UpdateProfileInput UpdateProfile { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!TryValidateCreateLobby())
        {
            ViewData["OpenModal"] = "create";
            await LoadAsync();
            return Page();
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            return RedirectToPage("/Index");
        }

        var lobby = await _lobbyService.CreateLobbyAsync(new LobbyCreateRequest(
            CreateLobby.Name,
            CreateLobby.MaxPlayers,
            CreateLobby.SmallBlind,
            CreateLobby.BigBlind), userId.Value);

        if (lobby is null)
        {
            StatusMessage = "Could not create lobby.";
            await LoadAsync();
            return Page();
        }

        var joinResult = await _lobbyService.JoinLobbyAsync(lobby.Id, userId.Value);
        if (joinResult != LobbyJoinResult.Success)
        {
            StatusMessage = "Lobby created, but you could not join it.";
        }

        StatusMessage = "Lobby created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostJoinAsync(Guid id)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return RedirectToPage("/Index");
        }

        var joinResult = await _lobbyService.JoinLobbyAsync(id, userId.Value);
        switch (joinResult)
        {
            case LobbyJoinResult.Success:
                StatusMessage = "Joined lobby.";
                return RedirectToPage("/Game/Index", new { id });
            case LobbyJoinResult.LobbyFull:
                StatusMessage = "Lobby is full.";
                break;
            case LobbyJoinResult.LobbyNotFound:
                StatusMessage = "Lobby not found.";
                break;
            default:
                StatusMessage = "Could not join lobby.";
                break;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        if (!TryValidateEditLobby())
        {
            ViewData["OpenModal"] = "edit";
            await LoadAsync();
            return Page();
        }

        var lobby = await _lobbyService.UpdateLobbyAsync(EditLobby.Id, new LobbyUpdateRequest(
            EditLobby.Name,
            EditLobby.MaxPlayers,
            EditLobby.SmallBlind,
            EditLobby.BigBlind));

        StatusMessage = lobby is not null ? "Lobby updated." : "Lobby not found.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var success = await _lobbyService.DeleteLobbyAsync(id);
        StatusMessage = success ? "Lobby deleted." : "Lobby not found.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        if (!TryValidateUpdateProfile())
        {
            ViewData["OpenModal"] = "updateProfile";
            await LoadAsync();
            return Page();
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            return RedirectToPage("/Index");
        }

        var user = await _userServices.UpdateUserAsync(userId.Value, new UserUpdateRequest(
            UpdateProfile.Username,
            UpdateProfile.Email,
            UpdateProfile.Password));

        if (user is null)
        {
            StatusMessage = "Could not update profile.";
            return RedirectToPage();
        }

        await _sessionService.SignInAsync(HttpContext, user);
        StatusMessage = "Profile updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAccountAsync()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return RedirectToPage("/Index");
        }

        var success = await _userServices.DeleteUserAsync(userId.Value);
        if (!success)
        {
            StatusMessage = "Account not found.";
            return RedirectToPage();
        }

        await _sessionService.SignOutAsync(HttpContext);
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await _sessionService.SignOutAsync(HttpContext);
        return RedirectToPage("/Index");
    }

    private async Task LoadAsync()
    {
        var userId = User.GetUserId();
        var user = userId.HasValue
            ? await _userServices.GetUserByIdAsync(userId.Value)
            : null;

        Profile = user is not null
            ? new ProfileView(user.Username, user.Email)
            : new ProfileView();

        UpdateProfile = new UpdateProfileInput
        {
            Username = Profile.Username,
            Email = Profile.Email
        };

        var lobbies = await _lobbyService.GetAllLobbiesAsync(includePlayers: true);
        var pinnedLobbyId = user?.LobbyId;

        Lobbies = lobbies.Select(lobby => new LobbyListItem(
            lobby.Id,
            lobby.Name,
            lobby.Players.Count,
            lobby.MaxPlayers,
            lobby.SmallBlind,
            lobby.BigBlind,
            pinnedLobbyId.HasValue && pinnedLobbyId.Value == lobby.Id)).ToList();
    }

    private bool TryValidateCreateLobby()
    {
        ModelState.Clear();
        return TryValidateModel(CreateLobby, nameof(CreateLobby));
    }

    private bool TryValidateEditLobby()
    {
        ModelState.Clear();
        return TryValidateModel(EditLobby, nameof(EditLobby));
    }

    private bool TryValidateUpdateProfile()
    {
        ModelState.Clear();
        var isValid = TryValidateModel(UpdateProfile, nameof(UpdateProfile));

        if (!string.IsNullOrWhiteSpace(UpdateProfile.Password) && UpdateProfile.Password.Length < 6)
        {
            ModelState.AddModelError(
                $"{nameof(UpdateProfile)}.{nameof(UpdateProfile.Password)}",
                "Password must be at least 6 characters.");
            isValid = false;
        }

        return isValid;
    }

    public record LobbyListItem(
        Guid Id,
        string Name,
        int CurrentPlayers,
        int MaxPlayers,
        int SmallBlind,
        int BigBlind,
        bool IsPinned = false)
    {
        public bool IsFull => CurrentPlayers >= MaxPlayers;
    }

    public record ProfileView(string Username = "", string Email = "");

    public class CreateLobbyInput
    {
        [Required, StringLength(60, MinimumLength = 3)]
        public string Name { get; set; } = "Lobby";

        [Range(2, 10)]
        public int MaxPlayers { get; set; } = 6;

        [Range(1, 100000)]
        public int SmallBlind { get; set; } = 25;

        [Range(1, 100000)]
        public int BigBlind { get; set; } = 50;
    }

    public class EditLobbyInput
    {
        [Required]
        public Guid Id { get; set; }

        [Required, StringLength(60, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [Range(2, 10)]
        public int MaxPlayers { get; set; } = 6;

        [Range(1, 100000)]
        public int SmallBlind { get; set; } = 25;

        [Range(1, 100000)]
        public int BigBlind { get; set; } = 50;
    }

    public class UpdateProfileInput
    {
        [Required, StringLength(32, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(64)]
        public string? Password { get; set; }
    }
}
