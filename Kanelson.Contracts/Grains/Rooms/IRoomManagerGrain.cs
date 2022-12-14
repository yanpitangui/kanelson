namespace Kanelson.Contracts.Grains.Rooms;

public interface IRoomManagerGrain : IGrainWithIntegerKey
{
    Task Register(string room);
    Task<bool> Exists(string roomId);
    Task<ImmutableArray<string>> GetAll();
    Task Unregister(string room);
}