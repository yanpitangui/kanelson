using Orleans;

namespace Shared.Grains;

public interface IGameGrain : IGrainWithGuidKey
{
    public Task SetBase(string name, string ownerId);
    Task Start();
    Task<string> GetOwner();
    Task JoinGame(string userId);
}