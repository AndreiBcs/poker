using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Source.Domain.Entities;

namespace Source.Infrastructure.Authentication
{
    public class UserSessionService
    {
        public Task SignInAsync(HttpContext httpContext, User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            return httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        public Task SignOutAsync(HttpContext httpContext)
            => httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
