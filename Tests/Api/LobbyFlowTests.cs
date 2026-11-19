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
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var response = await client.PostAsJsonAsync(
            "/api/players",
            new { nickname });
        
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Add("X-Test-Player-Id", body.GetProperty("id").GetString());
        client.DefaultRequestHeaders.Add("Cookie", response.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        return client;
    }
    private static async Task<Guid> Create(HttpClient host, int maxPlayers = 6)
    {
        var response = await host.PostAsJsonAsync(
            "/api/lobbies",
            new
            {
                name = "Test table",
                password = "table-pass",
                maxPlayers
            });
        
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    
    private static Guid Id(HttpClient client) => 
        Guid.Parse(client.DefaultRequestHeaders.GetValues("X-Test-Player-Id").Single());
    
    private sealed class LiveClient : IAsyncDisposable
    {
        private readonly HubConnection _connection;
        private readonly Channel<JsonElement> _states = Channel.CreateUnbounded<JsonElement>();

        public LiveClient(WebApplicationFactory<Program> factory, HttpClient player)
        {
            _connection = new HubConnectionBuilder().WithUrl(
                "http://localhost/api/live", 
                options => 
                {
                    options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                    options.Headers.Add("Cookie", player.DefaultRequestHeaders.GetValues("Cookie").Single());
                }).Build();
            
            _connection.On<JsonElement>("State", state => _states.Writer.TryWrite(state));
        }

        public async Task Start(Guid roomId)
        {
            await _connection.StartAsync();
            await _connection.InvokeAsync("JoinRoom", roomId);
        }

        public async Task<JsonElement> Snapshot()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            return await _states.Reader.ReadAsync(timeout.Token);
        }

        public Task Join(Guid roomId) => _connection.InvokeAsync("JoinRoom", roomId);
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
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        
        Assert.Equal(
            HttpStatusCode.BadRequest, 
            (await client.PostAsJsonAsync("/api/players", new { nickname = " " })).StatusCode);
        
        using var host = await Player(factory, "Host");
        
        foreach (var count in new[] { 1, 11 })
            Assert.Equal(
                HttpStatusCode.BadRequest, 
                (await host.PostAsJsonAsync("/api/lobbies",
                    new
                    {
                        name = "x",
                        password = "p",
                        maxPlayers = count 
                        
                    })).StatusCode);
        
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
        await using var factory = new WebApplicationFactory<Program>();
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
        await using var factory = new WebApplicationFactory<Program>();
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
        await using var factory = new WebApplicationFactory<Program>();
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
        await using var factory = new WebApplicationFactory<Program>();
        using var host = await Player(factory, "Host");
        using var guest = await Player(factory, "Guest");
        var id = await Create(host);
        (await guest.PostAsJsonAsync($"/api/lobbies/{id}/join", new { password = "table-pass" })).EnsureSuccessStatusCode();
        (await host.PostAsync($"/api/lobbies/{id}/start", null)).EnsureSuccessStatusCode();
        var waiting = await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}");
        Assert.Equal("connecting", waiting.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, waiting.GetProperty("game").ValueKind);
        Assert.Empty((await host.GetFromJsonAsync<JsonElement>("/api/lobbies")).EnumerateArray());
        await using var hostSocket = new LiveClient(factory, host);
        await hostSocket.Start(id);
        Assert.Equal(JsonValueKind.Null, (await Snapshot(hostSocket)).GetProperty("game").ValueKind);
        await using var guestSocket = new LiveClient(factory, guest);
        await guestSocket.Start(id);
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
        await using var reconnected = new LiveClient(factory, guest);
        await reconnected.Start(id);
        Assert.Equal("playing", (await UntilGame(reconnected)).GetProperty("status").GetString());
    }
    [Fact]
    public async Task HubRejectsNonMembersAndRepeatedJoins()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var host = await Player(factory, "Host");
        using var outsider = await Player(factory, "Outsider");
        var id = await Create(host);
        await using var socket = new LiveClient(factory, outsider);
        await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(() => socket.Start(id));
        await using var member = new LiveClient(factory, host);
        await member.Start(id);
        await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(() => member.Join(id));
    }

    [Fact]
    public async Task ClosingOneOfTwoConnectionsKeepsSeatConnected()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var host = await Player(factory, "Host");
        var id = await Create(host);
        await using var first = new LiveClient(factory, host);
        await using var second = new LiveClient(factory, host);
        await first.Start(id);
        await second.Start(id);
        await first.Stop();
        var state = await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}");
        Assert.True(state.GetProperty("players")[0].GetProperty("connected").GetBoolean());
    }

    [Fact]
    public async Task SavedGameRequiresItsSeatCookieAndOneActiveMembership()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var host = await Player(factory, "Host");
        using var guest = await Player(factory, "Guest");
        var id = await Create(host);
        (await guest.PostAsJsonAsync($"/api/lobbies/{id}/join", new { password = "table-pass" })).EnsureSuccessStatusCode();
        await using var hostSocket = new LiveClient(factory, host);
        await using var guestSocket = new LiveClient(factory, guest);
        await hostSocket.Start(id);
        await guestSocket.Start(id);
        (await host.PostAsync($"/api/lobbies/{id}/start", null)).EnsureSuccessStatusCode();
        var saved = (await host.GetFromJsonAsync<JsonElement>("/api/players/me")).GetProperty("savedGame");
        Assert.Equal(id, saved.GetProperty("gameId").GetGuid());
        var sessionId = saved.GetProperty("sessionId").GetGuid();
        var path = $"/api/games/{id}/sessions/{sessionId}";
        Assert.Equal(sessionId, (await host.GetFromJsonAsync<JsonElement>(path)).GetProperty("game").GetProperty("sessionId").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await host.GetAsync($"/api/games/{id}/sessions/{Guid.NewGuid()}")).StatusCode);

        using var forged = factory.CreateClient();
        forged.DefaultRequestHeaders.Add("X-Player-Id", Id(host).ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await forged.GetAsync(path)).StatusCode);
        using var outsider = await Player(factory, "Outsider");
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync(path)).StatusCode);
        using var crossOrigin = new HttpRequestMessage(HttpMethod.Post, $"/api/lobbies/{id}/leave");
        crossOrigin.Headers.Add("Origin", "http://localhost:9000");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.SendAsync(crossOrigin)).StatusCode);

        (await host.PostAsync($"/api/lobbies/{id}/step-away", null)).EnsureSuccessStatusCode();
        var steppedAway = await host.GetFromJsonAsync<JsonElement>(path);
        var hostSeat = steppedAway.GetProperty("game").GetProperty("players").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == Id(host));
        Assert.True(hostSeat.GetProperty("isSittingOut").GetBoolean());
        Assert.Equal(1975, hostSeat.GetProperty("chips").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await host.PostAsJsonAsync("/api/players", new { nickname = "Other name" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.PostAsJsonAsync("/api/lobbies", new { name = "Other", password = "pass", maxPlayers = 2 })).StatusCode);

        (await host.PostAsync($"/api/lobbies/{id}/leave", null)).EnsureSuccessStatusCode();
        Assert.Equal(Id(guest), (await guest.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}")).GetProperty("ownerId").GetGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await host.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync(path)).StatusCode);
        Assert.NotEqual(id, await Create(host));
    }

    [Fact]
    public async Task BustedSeatCanReconnectAndDoesNotBlockAnotherHand()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var host = await Player(factory, "Host");
        using var smallBlind = await Player(factory, "Small blind");
        using var bigBlind = await Player(factory, "Big blind");
        var created = await host.PostAsJsonAsync("/api/lobbies", new
        {
            name = "Spectator table", password = "table-pass", maxPlayers = 3, smallBlind = 1000, bigBlind = 2000
        });
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        foreach (var guest in new[] { smallBlind, bigBlind })
            (await guest.PostAsJsonAsync($"/api/lobbies/{id}/join", new { password = "table-pass" })).EnsureSuccessStatusCode();
        await using var hostSocket = new LiveClient(factory, host);
        await using var smallBlindSocket = new LiveClient(factory, smallBlind);
        await using var bigBlindSocket = new LiveClient(factory, bigBlind);
        await hostSocket.Start(id);
        await smallBlindSocket.Start(id);
        await bigBlindSocket.Start(id);
        (await host.PostAsync($"/api/lobbies/{id}/start", null)).EnsureSuccessStatusCode();
        (await bigBlind.PostAsync($"/api/lobbies/{id}/step-away", null)).EnsureSuccessStatusCode();
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/actions", new { action = "fold" })).EnsureSuccessStatusCode();
        await bigBlindSocket.Stop();

        (await host.PostAsync($"/api/lobbies/{id}/start", null)).EnsureSuccessStatusCode();
        var saved = (await bigBlind.GetFromJsonAsync<JsonElement>("/api/players/me")).GetProperty("savedGame");
        var sessionId = saved.GetProperty("sessionId").GetGuid();
        (await bigBlind.GetAsync($"/api/games/{id}/sessions/{sessionId}")).EnsureSuccessStatusCode();
        await using var spectator = new LiveClient(factory, bigBlind);
        await spectator.Start(id);
        var state = await UntilGame(spectator);
        Assert.Equal("playing", state.GetProperty("status").GetString());
        var seats = state.GetProperty("game").GetProperty("players").EnumerateArray().ToList();
        Assert.Equal(0, seats.Single(p => p.GetProperty("id").GetGuid() == Id(bigBlind)).GetProperty("chips").GetInt32());
        Assert.All(seats, seat => Assert.Empty(seat.GetProperty("holeCards").EnumerateArray()));
        Assert.Equal(HttpStatusCode.BadRequest, (await bigBlind.PostAsJsonAsync($"/api/lobbies/{id}/actions", new { action = "fold" })).StatusCode);
    }

}
