using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Source.Features.Game;
using Source.Features.Lobby;
using Source.Infrastructure.Authentication;

namespace Source.Pages.Game;

[Authorize]
public class IndexModel : PageModel
{
    private readonly LobbyService _lobbyService;
    private readonly GameService _gameService;

    public IndexModel(LobbyService lobbyService, GameService gameService)
    {
        _lobbyService = lobbyService;
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

        var lobby = await _lobbyService.GetLobbyByIdAsync(id.Value, includePlayers: true);
        if (lobby is null)
        {
            StatusMessage = "Lobby not found.";
            return Page();
        }

        LobbyId = lobby.Id;
        LobbyName = lobby.Name;
        SmallBlind = lobby.SmallBlind;
        BigBlind = lobby.BigBlind;
        IsLeader = lobby.OwnerUserId == userId.Value;

        var state = await _gameService.GetStateAsync(id.Value, userId.Value);
        if (state is not null)
        {
            // If the game has started, we have a state
        }

        return Page();
    }
}
