using Orleans;
using Orleans.Runtime;
using Shared;
using Shared.Models;

namespace Kanelson.Grains;

public class GameGrain : Grain, IGameGrain
{
    private readonly IPersistentState<GameState> _game;

    public GameGrain(
        [PersistentState("game", "kanelson-storage")]
        IPersistentState<GameState> game)
    {
        _game = game;
    }

    public async Task SetName(string name)
    {
        _game.State.Name = name;
        await _game.WriteStateAsync();
    }
}
