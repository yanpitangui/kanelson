using System.Collections.Immutable;
using Orleans;
using Orleans.Runtime;
using Shared.Grains.Templates;

namespace Kanelson.Grains.Templates;

public class TemplateManagerGrain : Grain, ITemplateManagerGrain
{
    private readonly IPersistentState<TemplateManagerState> _state;

    public TemplateManagerGrain(
        [PersistentState("templateIndex", "kanelson-storage")]
        IPersistentState<TemplateManagerState> state)
    {
        _state = state;
    }

    public async Task RegisterAsync(Guid itemKey)
    {
        _state.State.Items.Add(itemKey);
        await _state.WriteStateAsync();
    }

    public async Task UnregisterAsync(Guid itemKey)
    {
        _state.State.Items.Remove(itemKey);
        await _state.WriteStateAsync();
    }

    public Task<ImmutableArray<Guid>> GetAllAsync() =>
        Task.FromResult(ImmutableArray.CreateRange(_state.State.Items));
}

[Serializable]
public class TemplateManagerState
{
    public HashSet<Guid> Items { get; set; } = new();

}