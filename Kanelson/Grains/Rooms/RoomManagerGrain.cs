using System.Collections.Immutable;
using Orleans.Runtime;
using Kanelson.Contracts.Grains.Rooms;

namespace Kanelson.Grains.Rooms;

public class RoomManagerGrain : IRoomManagerGrain
{
    private readonly IPersistentState<RoomManagerState> _state;

    public RoomManagerGrain(
        [PersistentState("roomIndex", "kanelson-storage")]
        IPersistentState<RoomManagerState> state)
    {
        _state = state;
    }
    
    public async Task Register(string room)
    {
        _state.State.Items.Add(room);
        await _state.WriteStateAsync();
    }

    public Task<bool> Exists(string roomId)
    {
        return Task.FromResult(_state.State.Items.Contains(roomId));
    }

    public Task<ImmutableArray<string>> GetAll()
    {
        return Task.FromResult(_state.State.Items.ToImmutableArray());
    }

    public async Task Unregister(string room)
    {
        _state.State.Items.Remove(room);
        await _state.WriteStateAsync();
    }
}

[GenerateSerializer]
public class RoomManagerState
{
    [Id(0)]
    public HashSet<string> Items { get; set; } = new();
}