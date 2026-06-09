using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Source.Features.Game;
using Source.Infrastructure.Authentication;

namespace Source.Pages.Game;

[Authorize]
public class IndexModel : PageModel
{
    private readonly GameService _gameService;

    public IndexModel(GameService gameService)
    {
        _gameService = gameService;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    public Guid? LobbyId { get; private set; }
    public string LobbyName { get; private set; } = string.Empty;
    public int SmallBlind { get; private set; }
    public int BigBlind { get; private set; }
    public bool IsLeader { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id is null)
        {
            StatusMessage = "No lobby selected.";
            return Page();
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            return RedirectToPage("/Index");
        }

        if (!await _gameService.IsUserInLobbyAsync(id.Value, userId.Value))
        {
            StatusMessage = "You are not in this lobby.";
            return RedirectToPage("/Lobby/Index");
        }

        var lobbyInfo = await _gameService.GetLobbyInfoAsync(id.Value);
        if (lobbyInfo is null)
        {
            StatusMessage = "Lobby not found.";
            return Page();
        }

        LobbyId = lobbyInfo.LobbyId;
        LobbyName = lobbyInfo.LobbyName;
        SmallBlind = lobbyInfo.SmallBlind;
        BigBlind = lobbyInfo.BigBlind;
        IsLeader = lobbyInfo.OwnerUserId == userId.Value;

        return Page();
    }
}
