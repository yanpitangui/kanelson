using Orleans;

namespace Shared.Grains.Rooms;

public interface IRoomManagerGrain : IGrainWithIntegerKey
{
    Task Register(string room);
    Task<bool> Exists(string roomId);
    Task<ImmutableArray<string>> GetAll();
}