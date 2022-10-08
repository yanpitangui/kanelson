using Orleans;
using Shared;

namespace Kanelson.Services;

public class GameService
{
    private readonly IGrainFactory _grainFactory;

    public GameService(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task<Guid> CreateGame(string name)
    {
        var grain = _grainFactory.GetGrain<IRoomGrain>(Guid.NewGuid());
        await grain.SetName(name);
        return grain.GetPrimaryKey();
    }
}