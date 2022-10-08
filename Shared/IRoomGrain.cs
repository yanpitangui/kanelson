using Orleans;

namespace Shared;

public interface IRoomGrain : IGrainWithGuidKey
{
    public Task SetName(string name);
}