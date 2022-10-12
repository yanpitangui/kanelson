using Orleans;

namespace Shared.Grains;

public interface IGameManagerGrain : IGrainWithIntegerKey
{
    public Task RegisterGame(Guid id);

    public Task UnregisterGame(Guid id);

    public Task<bool> RoomExists(Guid id);
}