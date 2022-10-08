using Orleans;
using Orleans.Runtime;
using Shared;
using Shared.Models;

namespace Kanelson.Grains;

public class RoomGrain : Grain, IRoomGrain
{
    private readonly IPersistentState<RoomState> _state;

    public RoomGrain(
        [PersistentState("room", "kanelson-storage")]
        IPersistentState<RoomState> state)
    {
        _state = state;
    }

    public async Task SetName(string name)
    {
        _state.State.Name = name;
        await _state.WriteStateAsync();
    }
}
