using Orleans;
using Orleans.Runtime;
using Shared.Grains.Templates;
using Shared.Models;

namespace Kanelson.Grains.Templates;

public class TemplateGrain : Grain, ITemplateGrain
{
    private readonly IPersistentState<TemplateState> _state;

    public TemplateGrain(
        [PersistentState("template", "kanelson-storage")]
        IPersistentState<TemplateState> state)
    {
        _state = state;
    }

    public async Task SetBase(Template template, string ownerId)
    {
        _state.State.Template = template;
        _state.State.OwnerId = ownerId;
        await _state.WriteStateAsync();
    }

    public Task<TemplateSummary> GetSummary()
    {
        return Task.FromResult(new TemplateSummary(this.GetPrimaryKey(), _state.State.Template.Name));
    }

    public Task<Template> GetById()
    {
        return Task.FromResult(_state.State.Template);
    }

    public Task<string> GetOwner()
    {
        return Task.FromResult(_state.State.OwnerId);
    }

    public async Task Delete()
    {
        await _state.ClearStateAsync();
        this.DeactivateOnIdle();
    }
}

[Serializable]
public record TemplateState
{
    public string OwnerId { get; set; } = null!;
    public Template Template { get; set; } = null!;
}