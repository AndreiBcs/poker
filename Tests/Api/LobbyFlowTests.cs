using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Program = Api.Program;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using System.Threading.Channels;
using Xunit;

namespace Core.Tests.Api;

[Trait("Category", "Integration")]
public class LobbyFlowTests
{
    private static async Task<HttpClient> Player(WebApplicationFactory<Program> factory, string nickname)
    {
        var client = factory.CreateClient();
        var response = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/players", new { nickname });
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
    private sealed class LiveClient : IAsyncDisposable
    {
        private readonly HubConnection _connection;
        private readonly Channel<JsonElement> _states = Channel.CreateUnbounded<JsonElement>();

        public LiveClient(WebApplicationFactory<Program> factory)
        {
            _connection = new HubConnectionBuilder().WithUrl("http://localhost/api/live", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            }).Build();
            _connection.On<JsonElement>("State", state => _states.Writer.TryWrite(state));
        }

        public async Task Start(Guid roomId, Guid playerId)
        {
            await _connection.StartAsync();
            await _connection.InvokeAsync("JoinRoom", roomId, playerId);
        }

        public async Task<JsonElement> Snapshot()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            return await _states.Reader.ReadAsync(timeout.Token);
        }

        public Task Join(Guid roomId, Guid playerId) => _connection.InvokeAsync("JoinRoom", roomId, playerId);
        public Task Stop() => _connection.StopAsync();
        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }

    private static Task<JsonElement> Snapshot(LiveClient socket) => socket.Snapshot();
    private static async Task<JsonElement> UntilGame(LiveClient socket)
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
        Assert.Equal(HttpStatusCode.BadRequest, (await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/players", new { nickname = " " })).StatusCode);
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
        await using var hostSocket = new LiveClient(factory);
        await hostSocket.Start(id, Id(host));
        Assert.Equal(JsonValueKind.Null, (await Snapshot(hostSocket)).GetProperty("game").ValueKind);
        await using var guestSocket = new LiveClient(factory);
        await guestSocket.Start(id, Id(guest));
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
        await guestSocket.Stop();
        JsonElement paused = default;
        for (var i = 0; i < 30; i++)
        {
            paused = await Snapshot(hostSocket);
            if (paused.GetProperty("status").GetString() == "paused") break;
        }
        Assert.Equal("paused", paused.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await host.PostAsJsonAsync($"/api/lobbies/{id}/actions", new { action = "fold" })).StatusCode);
        await using var reconnected = new LiveClient(factory);
        await reconnected.Start(id, Id(guest));
        Assert.Equal("playing", (await UntilGame(reconnected)).GetProperty("status").GetString());
    }
    [Fact]
    public async Task HubRejectsNonMembersAndRepeatedJoins()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var host = await Player(factory, "Host");
        using var outsider = await Player(factory, "Outsider");
        var id = await Create(host);
        await using var socket = new LiveClient(factory);
        await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(() => socket.Start(id, Id(outsider)));
        await using var member = new LiveClient(factory);
        await member.Start(id, Id(host));
        await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(() => member.Join(id, Id(host)));
    }

    [Fact]
    public async Task ClosingOneOfTwoConnectionsKeepsSeatConnected()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var host = await Player(factory, "Host");
        var id = await Create(host);
        await using var first = new LiveClient(factory);
        await using var second = new LiveClient(factory);
        await first.Start(id, Id(host));
        await second.Start(id, Id(host));
        await first.Stop();
        var state = await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}");
        Assert.True(state.GetProperty("players")[0].GetProperty("connected").GetBoolean());
    }

}
