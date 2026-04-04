using Microsoft.AspNetCore.Mvc;
using User_Microservice.Data;
using User_Microservice.DTOs;
using User_Microservice.Models;
using User_Microservice.Services;

namespace User_Microservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(DatabaseContext context, AuthService authService) : ControllerBase
    {
        private readonly DatabaseContext _context = context;
        private readonly AuthService _authService = authService;

        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            if (_context.Users.Any(u => u.Email == dto.Email || u.UserName == dto.UserName))
                return Conflict("User already exists");

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = _authService.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User created");
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);

            if (user == null || !_authService.VerifyPassword(user.PasswordHash, dto.Password))
                return Unauthorized("Invalid credentials");

            return Ok("Login success (JWT next step)");
        }
    }
}
