using System.Buffers;
using System.Collections.Immutable;
using Akka.Hosting;
using Orleans;
using Kanelson.Contracts.Grains.Templates;
using Kanelson.Contracts.Models;

namespace Kanelson.Services;

public class TemplateService : ITemplateService
{
    private readonly ActorRegistry _actorRegistry;
    private readonly IUserService _userService;


    public TemplateService(ActorRegistry actorRegistry, IUserService userService)
    {
        _actorRegistry = actorRegistry;
        _userService = userService;
    }

    public async Task UpsertTemplate(Template template)
    {
        var manager = _client.GetGrain<ITemplateManagerGrain>(_userService.CurrentUser);
        var templateGrain = _client.GetGrain<ITemplateGrain>(template.Id);
        await templateGrain.SetBase(template, _userService.CurrentUser);
        await manager.RegisterAsync(templateGrain.GetPrimaryKey());
    }

    public async Task<ImmutableArray<TemplateSummary>> GetTemplates()
    {
        var manager = _client.GetGrain<ITemplateManagerGrain>(_userService.CurrentUser);
        var keys = await manager.GetAllAsync();
        // fan out to get the individual items from the cluster in parallel
        var tasks = ArrayPool<Task<TemplateSummary>>.Shared.Rent(keys.Length);
        try
        {
            // issue all individual requests at the same time
            for (var i = 0; i < keys.Length; ++i)
            {
                tasks[i] = _client.GetGrain<ITemplateGrain>(keys[i]).GetSummary();
            }

            // build the result as requests complete
            var result = ImmutableArray.CreateBuilder<TemplateSummary>(keys.Length);
            for (var i = 0; i < keys.Length; ++i)
            {
                var item = await tasks[i];
                
                result.Add(item);
            }
            return result.ToImmutableArray();
        }
        finally
        {
            ArrayPool<Task<TemplateSummary>>.Shared.Return(tasks);
        }
    }

    public async Task<Template> GetTemplate(Guid id)
    {
        var manager = _client.GetGrain<ITemplateManagerGrain>(_userService.CurrentUser);
        var templates = await manager.GetAllAsync();
        if (!templates.Contains(id))
        {
            throw new KeyNotFoundException();
        }

        return await _client.GetGrain<ITemplateGrain>(id).Get();
    }

    public async Task DeleteTemplate(Guid id)
    {
        var manager = _client.GetGrain<ITemplateManagerGrain>(_userService.CurrentUser);
        if (!await manager.KeyExists(id))
        {
            throw new KeyNotFoundException();
        }

        var grain = _client.GetGrain<ITemplateGrain>(id);
        await grain.Delete();
        await manager.UnregisterAsync(id);

    }
}

public interface ITemplateService
{
    Task UpsertTemplate(Template template);
    Task<ImmutableArray<TemplateSummary>> GetTemplates();
    
    Task<Template> GetTemplate(Guid id);
    Task DeleteTemplate(Guid id);
}