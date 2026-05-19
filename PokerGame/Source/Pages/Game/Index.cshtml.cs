using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Source.Pages.Game;

[Authorize]
public class IndexModel : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public Guid? LobbyId { get; private set; }

    public void OnGet(Guid? id)
    {
        LobbyId = id;
    }
}
