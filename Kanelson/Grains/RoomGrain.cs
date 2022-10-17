using Orleans;
using Shared.Grains.Rooms;

namespace Kanelson.Grains;

public class RoomGrain : Grain, IRoomGrain
{
    public async Task<bool> JoinRoom(Guid id)
    {
        throw new NotImplementedException();
    }
}

[Serializable]
public record RoomState
{
    
}

public enum RoomStatus
{
    Created,
    Started,
    Finished,
    Abandoned
}