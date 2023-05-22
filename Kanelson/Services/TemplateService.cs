using System.Buffers;
using System.Collections.Immutable;
using Akka.Actor;
using Kanelson.Contracts.Models;
using Kanelson.Grains.Templates;

namespace Kanelson.Services;

public class TemplateService : ITemplateService
{
    private readonly ActorSystem _actorSystem;
    private readonly IUserService _userService;


    public TemplateService(ActorSystem actorSystem, IUserService userService)
    {
        _actorSystem = actorSystem;
        _userService = userService;
    }

    public async Task UpsertTemplate(Template template)
    {

        var manager = _actorSystem.ActorOf(TemplateManagerActor.Props(_userService.CurrentUser));
        var exists = await manager.Ask<bool>(new Exists(template.Id));
        if (!exists)
        {
            manager.Tell(new Register(template.Id));
        }
        var actor = await manager.Ask<IActorRef>(new GetRef(template.Id));
        actor.Tell(new Upsert(template, _userService.CurrentUser));
    }

    public async Task<ImmutableArray<TemplateSummary>> GetTemplates()
    {
        var manager = _actorSystem.ActorOf(TemplateManagerActor.Props(_userService.CurrentUser));
        
        var keys = await manager.Ask<ImmutableArray<Guid>>(new GetAll());
        // fan out to get the individual items from the cluster in parallel
        var tasks = ArrayPool<Task<TemplateSummary>>.Shared.Rent(keys.Length);
        try
        {
            // issue all individual requests at the same time
            for (var i = 0; i < keys.Length; ++i)
            {
                tasks[i] = _actorSystem.ActorOf(TemplateActor.Props(keys[i])).Ask<TemplateSummary>(new GetSummary());
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
        var manager = _actorSystem.ActorOf(TemplateManagerActor.Props(_userService.CurrentUser));
        var exists = await manager.Ask<bool>(new Exists(id));
        if (!exists)
        {
            throw new KeyNotFoundException();
        }

        var actorRef = await manager.Ask<IActorRef>(new GetRef(id));
        return await actorRef.Ask<Template>(new GetTemplate());
    }

    public async Task DeleteTemplate(Guid id)
    {
        var manager = _actorSystem.ActorOf(TemplateManagerActor.Props(_userService.CurrentUser));
        var exists = await manager.Ask<bool>(new Exists(id));
        if (!exists)
        {
            throw new KeyNotFoundException();
        }
        manager.Tell(new Unregister(id));
    }
}

public interface ITemplateService
{
    Task UpsertTemplate(Template template);
    Task<ImmutableArray<TemplateSummary>> GetTemplates();
    
    Task<Template> GetTemplate(Guid id);
    Task DeleteTemplate(Guid id);
}