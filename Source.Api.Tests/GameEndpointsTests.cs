using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Source.Api.Tests;

public class GameEndpointsTests : ApiTestBase
{
    public GameEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task StartGame_WhenLobbyMissing_ReturnsNotFound()
    {
        await ResetDatabaseAsync();
        var user = await SeedUserAsync("starter", "starter@example.com");
        var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/games/{Guid.NewGuid()}/start");
        AddAuthHeaders(request, user);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetState_WhenUserNotInLobby_ReturnsForbidden()
    {
        await ResetDatabaseAsync();
        var user = await SeedUserAsync("viewer", "viewer@example.com");
        var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/games/{Guid.NewGuid()}/state");
        AddAuthHeaders(request, user);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
