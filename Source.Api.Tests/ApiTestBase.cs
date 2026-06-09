using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Source.Domain.Entities;
using Source.Infrastructure.Persistence;
using Xunit;


namespace Source.Api.Tests;

public abstract class ApiTestBase : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    protected ApiTestBase(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    protected HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.RemoveRange(db.Users);
        db.Lobbies.RemoveRange(db.Lobbies);
        db.Games.RemoveRange(db.Games);

        await db.SaveChangesAsync();
    }

    protected async Task<User> SeedUserAsync(string username, string email, string password = "Passw0rd!")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    protected async Task<Lobby> SeedLobbyAsync(string name, Guid ownerUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var lobby = new Lobby
        {
            Id = Guid.NewGuid(),
            Name = name,
            OwnerUserId = ownerUserId,
            MaxPlayers = 6,
            SmallBlind = 50,
            BigBlind = 100
        };

        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        return lobby;
    }

    protected static void AddAuthHeaders(HttpRequestMessage request, User user)
    {
        request.Headers.Add(TestAuthHandler.UserIdHeader, user.Id.ToString());
        request.Headers.Add(TestAuthHandler.UserNameHeader, user.Username);
        request.Headers.Add(TestAuthHandler.EmailHeader, user.Email);
    }
}
