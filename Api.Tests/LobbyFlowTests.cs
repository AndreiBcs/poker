using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Api.Tests;

public class LobbyFlowTests
{
    private static async Task<HttpClient> Player(WebApplicationFactory<Program> factory, string nickname)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/players", new { nickname });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Add("X-Player-Id", body.GetProperty("id").GetString());
        return client;
    }
    private static async Task<Guid> Create(HttpClient host, int maxPlayers = 6)
    {
        var response = await host.PostAsJsonAsync("/api/lobbies", new { name = "Test table", password = "table-pass", maxPlayers });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private static Guid Id(HttpClient client) => Guid.Parse(client.DefaultRequestHeaders.GetValues("X-Player-Id").Single());
    private static async Task<JsonElement> Snapshot(WebSocket socket)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var stream = new System.IO.MemoryStream();
        var buffer = new byte[8192];
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, timeout.Token);
            Assert.NotEqual(WebSocketMessageType.Close, result.MessageType);
            stream.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);
        return JsonDocument.Parse(stream.ToArray()).RootElement.GetProperty("payload").Clone();
    }
    private static async Task<JsonElement> UntilGame(WebSocket socket)
    {
        for (var i = 0; i < 30; i++)
        {
            var state = await Snapshot(socket);
            if (state.GetProperty("game").ValueKind != JsonValueKind.Null) return state;
        }
        throw new Exception("No game started.");
    }

    [Fact]
    public async Task NicknamesAndLobbyCapacityAreValidated()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/players", new { nickname = " " })).StatusCode);
        using var host = await Player(factory, "Host");
        foreach (var count in new[] { 1, 11 })
            Assert.Equal(HttpStatusCode.BadRequest, (await host.PostAsJsonAsync("/api/lobbies", new { name = "x", password = "p", maxPlayers = count })).StatusCode);
        var id = await Create(host, 10);
        var state = await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}");
        Assert.Equal(10, state.GetProperty("maxPlayers").GetInt32());
        Assert.Equal(Id(host), state.GetProperty("ownerId").GetGuid());
        Assert.Single(state.GetProperty("players").EnumerateArray());
        Assert.False(state.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task PasswordMembershipCapacityAndHostControlsAreEnforced()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var host = await Player(factory, "Host");
        using var guest = await Player(factory, "Guest");
        using var outsider = await Player(factory, "Outsider");
        var id = await Create(host, 2);
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.PostAsJsonAsync($"/api/lobbies/{id}/join", new { password = "wrong" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.PostAsync($"/api/lobbies/{id}/start", null)).StatusCode);
        (await guest.PostAsJsonAsync($"/api/lobbies/{id}/join", new { password = "table-pass" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await outsider.PostAsJsonAsync($"/api/lobbies/{id}/join", new { password = "table-pass" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/api/lobbies/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.PostAsync($"/api/lobbies/{id}/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.DeleteAsync($"/api/lobbies/{id}/players/{Id(host)}")).StatusCode);
        (await host.DeleteAsync($"/api/lobbies/{id}/players/{Id(guest)}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.GetAsync($"/api/lobbies/{id}")).StatusCode);
    }

    [Fact]
    public async Task LastPlayerLeavingClosesLobbyAndHostTransfers()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var host = await Player(factory, "Host");
        using var guest = await Player(factory, "Guest");
        var id = await Create(host);
        (await guest.PostAsJsonAsync($"/api/lobbies/{id}/join", new { password = "table-pass" })).EnsureSuccessStatusCode();
        (await host.PostAsync($"/api/lobbies/{id}/leave", null)).EnsureSuccessStatusCode();
        var state = await guest.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}");
        Assert.Equal(Id(guest), state.GetProperty("ownerId").GetGuid());
        (await guest.PostAsync($"/api/lobbies/{id}/leave", null)).EnsureSuccessStatusCode();
        Assert.Empty((await host.GetFromJsonAsync<JsonElement>("/api/lobbies")).EnumerateArray());
    }

    [Fact]
    public async Task ConcurrentJoinsCannotOverfillLastSeat()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var host = await Player(factory, "Host");
        using var a = await Player(factory, "A");
        using var b = await Player(factory, "B");
        var id = await Create(host, 2);
        var responses = await Task.WhenAll(a.PostAsJsonAsync($"/api/lobbies/{id}/join", new { password = "table-pass" }),
            b.PostAsJsonAsync($"/api/lobbies/{id}/join", new { password = "table-pass" }));
        Assert.Single(responses, r => r.IsSuccessStatusCode);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GameWaitsForConnectionsHidesCardsAndPausesOnDisconnect()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var host = await Player(factory, "Host");
        using var guest = await Player(factory, "Guest");
        var id = await Create(host);
        (await guest.PostAsJsonAsync($"/api/lobbies/{id}/join", new { password = "table-pass" })).EnsureSuccessStatusCode();
        (await host.PostAsync($"/api/lobbies/{id}/start", null)).EnsureSuccessStatusCode();
        var waiting = await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}");
        Assert.Equal("connecting", waiting.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, waiting.GetProperty("game").ValueKind);
        Assert.Empty((await host.GetFromJsonAsync<JsonElement>("/api/lobbies")).EnumerateArray());
        var wsClient = factory.Server.CreateWebSocketClient();
        using var hostSocket = await wsClient.ConnectAsync(new Uri($"ws://localhost/api/lobbies/{id}/live?playerId={Id(host)}"), CancellationToken.None);
        Assert.Equal(JsonValueKind.Null, (await Snapshot(hostSocket)).GetProperty("game").ValueKind);
        using var guestSocket = await wsClient.ConnectAsync(new Uri($"ws://localhost/api/lobbies/{id}/live?playerId={Id(guest)}"), CancellationToken.None);
        var state = await UntilGame(hostSocket);
        var game = state.GetProperty("game");
        Assert.Equal("preFlop", game.GetProperty("phase").GetString());
        var seats = game.GetProperty("players").EnumerateArray().ToList();
        Assert.Equal(2, seats.Single(p => p.GetProperty("id").GetGuid() == Id(host)).GetProperty("holeCards").GetArrayLength());
        Assert.Empty(seats.Single(p => p.GetProperty("id").GetGuid() == Id(guest)).GetProperty("holeCards").EnumerateArray());
        var turn = game.GetProperty("currentTurnUserId").GetGuid();
        var actor = turn == Id(host) ? host : guest;
        var other = turn == Id(host) ? guest : host;
        Assert.Equal(HttpStatusCode.BadRequest, (await other.PostAsJsonAsync($"/api/lobbies/{id}/actions", new { action = "fold" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await actor.PostAsJsonAsync($"/api/lobbies/{id}/actions", new { action = "raise", amount = int.MaxValue })).StatusCode);
        (await actor.PostAsJsonAsync($"/api/lobbies/{id}/actions", new { action = "fold" })).EnsureSuccessStatusCode();
        var ended = await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}");
        Assert.Equal("waitingForPlayers", ended.GetProperty("game").GetProperty("phase").GetString());
        Assert.NotEqual(JsonValueKind.Null, ended.GetProperty("game").GetProperty("lastResult").ValueKind);
        (await host.PostAsync($"/api/lobbies/{id}/start", null)).EnsureSuccessStatusCode();
        await guestSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "leaving socket", CancellationToken.None);
        JsonElement paused = default;
        for (var i = 0; i < 30; i++)
        {
            paused = await Snapshot(hostSocket);
            if (paused.GetProperty("status").GetString() == "paused") break;
        }
        Assert.Equal("paused", paused.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await host.PostAsJsonAsync($"/api/lobbies/{id}/actions", new { action = "fold" })).StatusCode);
        using var reconnected = await wsClient.ConnectAsync(new Uri($"ws://localhost/api/lobbies/{id}/live?playerId={Id(guest)}"), CancellationToken.None);
        Assert.Equal("playing", (await UntilGame(reconnected)).GetProperty("status").GetString());
    }
}
