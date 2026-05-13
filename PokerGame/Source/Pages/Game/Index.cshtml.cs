using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Source.Pages.Game;

public class IndexModel : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public int? LobbyId { get; private set; }

    public void OnGet(int? id)
    {
        LobbyId = id;
    }
}