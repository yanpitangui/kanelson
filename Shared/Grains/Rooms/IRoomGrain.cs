using Orleans;

namespace Shared.Grains.Rooms;

public interface IRoomGrain : IGrainWithGuidKey
{
    Task<bool> JoinRoom(Guid id);

}