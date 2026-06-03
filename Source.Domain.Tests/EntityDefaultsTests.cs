using System;
using Source.Domain.Entities;
using Xunit;

namespace Source.Domain.Tests;

public class EntityDefaultsTests
{
    [Fact]
    public void Game_Defaults_AreInitialized()
    {
        var game = new Game();

        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.NotNull(game.Players);
        Assert.Empty(game.Players);
        Assert.Equal(50, game.SmallBlind);
        Assert.Equal(100, game.BigBlind);
    }

    [Fact]
    public void Lobby_Defaults_AreInitialized()
    {
        var lobby = new Lobby();

        Assert.NotEqual(Guid.Empty, lobby.Id);
        Assert.NotNull(lobby.Players);
        Assert.Empty(lobby.Players);
        Assert.Equal(6, lobby.MaxPlayers);
        Assert.Equal(50, lobby.SmallBlind);
        Assert.Equal(100, lobby.BigBlind);
        Assert.Null(lobby.OwnerUserId);
    }
}
