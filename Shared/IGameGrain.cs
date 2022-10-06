using Orleans;

namespace Shared;

public interface IGameGrain : IGrainWithGuidKey
{
    public Task SetName(string name);
}