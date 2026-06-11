using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Source.Features.Lobby;
using Source.Domain.Enums;
using Xunit;

namespace Source.Api.Tests;

public class LobbyEndpointsTests : ApiTestBase
{
    public LobbyEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateLobby_ReturnsCreated()
    {
        await ResetDatabaseAsync();
        var user = await SeedUserAsync("owner", "owner@example.com");
        var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/lobbies")
        {
            Content = JsonContent.Create(new LobbyCreateRequest("TestLobby", 6, 50, 100, GameType.TexasHoldem))
        };
        AddAuthHeaders(request, user);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var lobby = await response.Content.ReadFromJsonAsync<LobbyDetailResponse>();
        Assert.NotNull(lobby);
        Assert.Equal("TestLobby", lobby!.Name);
    }

    [Fact]
    public async Task JoinLobby_ReturnsNoContent()
    {
        await ResetDatabaseAsync();
        var user = await SeedUserAsync("player", "player@example.com");
        var lobby = await SeedLobbyAsync("Lobby", user.Id);
        var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/lobbies/{lobby.Id}/join");
        AddAuthHeaders(request, user);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
