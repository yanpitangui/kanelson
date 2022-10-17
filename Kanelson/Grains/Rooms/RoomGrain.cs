﻿using Orleans;
using Orleans.Runtime;
using Shared.Grains;
using Shared.Grains.Rooms;
using Shared.Models;

namespace Kanelson.Grains.Rooms;

public class RoomGrain : Grain, IRoomGrain
{
    private readonly IPersistentState<RoomState> _state;
    private readonly IGrainFactory _grainFactory;

    public RoomGrain([PersistentState("rooms", "kanelson-storage")]
        IPersistentState<RoomState> users, IGrainFactory grainFactory)
    {
        _state = users;
        _grainFactory = grainFactory;
    }
    public async Task SetBase(string roomName, string owner, Template template)
    {
        _state.State.Name = roomName;
        _state.State.OwnerId = owner;
        _state.State.Template = template;
        await _state.WriteStateAsync();
    }

    public async Task<RoomSummary> GetSummary()
    {
        var owner = (await _grainFactory.GetGrain<IUserManagerGrain>(0)
            .GetUserInfo(_state.State.OwnerId)).First();

        return new RoomSummary(this.GetPrimaryKeyString(), _state.State.Name, owner, _state.State.Status);
    }
}

[Serializable]
public record RoomState
{
    public string OwnerId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Template Template { get; set; } = null!;
    
    public RoomStatus Status { get; set; }

}
