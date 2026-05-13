using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Source.Pages.Lobby;

public class IndexModel : PageModel
{
    private static readonly List<LobbyListItem> LobbyStore = new()
    {
        new LobbyListItem(1, "Lobby #1", 5, 8, 50, 100),
        new LobbyListItem(2, "Lobby #2", 2, 6, 25, 50)
    };

    private static int _nextLobbyId = 3;

    [TempData]
    public string? StatusMessage { get; set; }

    public IReadOnlyList<LobbyListItem> Lobbies => LobbyStore;

    [BindProperty]
    public CreateLobbyInput CreateLobby { get; set; } = new();

    public IActionResult OnPostCreate()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var lobbyId = _nextLobbyId++;
        var lobby = new LobbyListItem(
            lobbyId,
            $"Lobby #{lobbyId}",
            1,
            CreateLobby.MaxPlayers,
            CreateLobby.SmallBlind,
            CreateLobby.BigBlind);

        LobbyStore.Add(lobby);
        StatusMessage = "Lobby created.";

        return RedirectToPage();
    }

    public IActionResult OnPostJoin(int id)
    {
        var lobbyIndex = LobbyStore.FindIndex(lobby => lobby.Id == id);
        if (lobbyIndex < 0)
        {
            StatusMessage = "Lobby not found.";
            return RedirectToPage();
        }

        var lobby = LobbyStore[lobbyIndex];
        if (lobby.IsFull)
        {
            StatusMessage = $"{lobby.Name} is full.";
            return RedirectToPage();
        }

        lobby = lobby with { CurrentPlayers = lobby.CurrentPlayers + 1 };
        LobbyStore[lobbyIndex] = lobby;

        StatusMessage = $"Joined {lobby.Name}.";
        return RedirectToPage("/Game/Index", new { id = lobby.Id });
    }

    public record LobbyListItem(
        int Id,
        string Name,
        int CurrentPlayers,
        int MaxPlayers,
        int SmallBlind,
        int BigBlind)
    {
        public bool IsFull => CurrentPlayers >= MaxPlayers;
    }

    public class CreateLobbyInput
    {
        [Range(2, 10)]
        public int MaxPlayers { get; set; } = 6;

        [Range(1, 100000)]
        public int SmallBlind { get; set; } = 25;

        [Range(1, 100000)]
        public int BigBlind { get; set; } = 50;
    }
}