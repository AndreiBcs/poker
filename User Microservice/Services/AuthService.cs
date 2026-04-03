using Microsoft.AspNetCore.Identity;
using User_Microservice.DTOs;


namespace User_Microservice.Services
{
    public class AuthService(IPasswordHasher<string> passwordHasher)
    {
        private readonly IPasswordHasher<string> _passwordHasher = passwordHasher;

        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(null, password);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
