using System.Collections.Immutable;
using Orleans;
using Orleans.Runtime;
using Shared.Grains.Games;
using Shared.Models;

namespace Kanelson.Grains.Games;

public class GameGrain : Grain, IGameGrain
{
    private readonly IPersistentState<GameState> _state;

    public GameGrain(
        [PersistentState("game", "kanelson-storage")]
        IPersistentState<GameState> state)
    {
        _state = state;
    }

    public async Task SetBase(Game game, string ownerId)
    {
        _state.State.Game = game;
        _state.State.OwnerId = ownerId;
        await _state.WriteStateAsync();
    }

    public Task<GameSummary> GetSummary()
    {
        return Task.FromResult(new GameSummary(this.GetPrimaryKey(), _state.State.Game.Name));
    }

    public Task<Game> GetGame()
    {
        return Task.FromResult(_state.State.Game);
    }

    public Task<string> GetOwner()
    {
        return Task.FromResult(_state.State.OwnerId);
    }

    public async Task Delete()
    {
        await _state.ClearStateAsync();
    }
}

[Serializable]
public record GameState
{
    public string OwnerId { get; set; } = null!;
    public Game Game { get; set; } = null!;
}