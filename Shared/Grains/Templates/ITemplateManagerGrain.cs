using Orleans;

namespace Shared.Grains.Templates;

public interface ITemplateManagerGrain : IGrainWithStringKey
{
    Task RegisterAsync(Guid itemKey);
    Task UnregisterAsync(Guid itemKey);

    Task<ImmutableArray<Guid>> GetAllAsync();

}