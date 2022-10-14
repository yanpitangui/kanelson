using System.Collections.Immutable;
using Orleans;
using Orleans.Runtime;
using Shared.Grains;

namespace Kanelson.Grains.Games;

public class GameManagerGrain : Grain, IGameManagerGrain
{
    private readonly IPersistentState<GameManagerState> _state;

    public GameManagerGrain(
        [PersistentState("gameIndex", "kanelson-storage")]
        IPersistentState<GameManagerState> state)
    {
        _state = state;
    }

    public async Task RegisterAsync(Guid itemKey)
    {
        _state.State.Items.Add(itemKey);
        await _state.WriteStateAsync();
    }

    public async Task UnregisterAsync(Guid itemKey)
    {
        _state.State.Items.Remove(itemKey);
        await _state.WriteStateAsync();
    }

    public Task<ImmutableArray<Guid>> GetAllAsync() =>
        Task.FromResult(ImmutableArray.CreateRange(_state.State.Items));
}

[Serializable]
public class GameManagerState
{
    public HashSet<Guid> Items { get; set; } = new();

}