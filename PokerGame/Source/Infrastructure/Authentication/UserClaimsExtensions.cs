using System.Security.Claims;

namespace Source.Infrastructure.Authentication
{
    public static class UserClaimsExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal principal)
        {
            var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idValue, out var id) ? id : null;
        }
    }
}
