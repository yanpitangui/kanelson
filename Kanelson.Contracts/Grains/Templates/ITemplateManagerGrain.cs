namespace Kanelson.Contracts.Grains.Templates;

public interface ITemplateManagerGrain : IGrainWithStringKey
{
    Task RegisterAsync(Guid itemKey);
    Task UnregisterAsync(Guid itemKey);

    Task<ImmutableArray<Guid>> GetAllAsync();

    Task<bool> KeyExists(Guid itemKey);
}