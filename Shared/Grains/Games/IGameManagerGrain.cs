using Orleans;

namespace Shared.Grains;

public interface IGameManagerGrain : IGrainWithStringKey
{
    Task RegisterAsync(Guid itemKey);
    Task UnregisterAsync(Guid itemKey);

    Task<ImmutableArray<Guid>> GetAllAsync();

}