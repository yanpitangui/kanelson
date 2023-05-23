using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Actor;
using Kanelson.Actors.Templates;
using Kanelson.Contracts.Models;

namespace Kanelson.Services;

public class TemplateService : ITemplateService
{
    private readonly ActorSystem _actorSystem;
    private readonly IUserService _userService;


    private static readonly ConcurrentDictionary<string, IActorRef> Managers;

    static TemplateService()
    {
        Managers = new();
    }

    public TemplateService(ActorSystem actorSystem, IUserService userService)
    {
        _actorSystem = actorSystem;
        _userService = userService;
    }

    public async Task UpsertTemplate(Template template)
    {
        var manager = GetOrCreateManagerRef();
        var actor = await manager.Ask<IActorRef>(new GetRef(template.Id));
        actor.Tell(new Upsert(template, _userService.CurrentUser));
    }

    public async Task<ImmutableArray<TemplateSummary>> GetTemplates()
    {
        var manager = GetOrCreateManagerRef();
        
        var keys = await manager.Ask<ImmutableArray<Guid>>(new GetAll());
        // fan out to get the individual items from the cluster in parallel
        var tasks = ArrayPool<Task<TemplateSummary>>.Shared.Rent(keys.Length);
        try
        {
            // issue all individual requests at the same time
            for (var i = 0; i < keys.Length; ++i)
            {
                var actor = await manager.Ask<IActorRef>(new GetRef(keys[i]));
                tasks[i] = actor.Ask<TemplateSummary>(new GetSummary());
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
        var manager = GetOrCreateManagerRef();
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
        var manager = GetOrCreateManagerRef();
        var exists = await manager.Ask<bool>(new Exists(id));
        if (!exists)
        {
            throw new KeyNotFoundException();
        }
        manager.Tell(new Unregister(id));
    }

    private IActorRef GetOrCreateManagerRef()
    {
        var exists = Managers.TryGetValue(_userService.CurrentUser, out var actorRef);
        if (Equals(actorRef, ActorRefs.Nobody) || !exists)
        {
            actorRef = _actorSystem.ActorOf(TemplateManagerActor.Props(_userService.CurrentUser));
        }

        Managers.AddOrUpdate(_userService.CurrentUser, (_) => actorRef!, 
            (_, _) => actorRef!);

        return actorRef!;

    }
}

public interface ITemplateService
{
    Task UpsertTemplate(Template template);
    Task<ImmutableArray<TemplateSummary>> GetTemplates();
    
    Task<Template> GetTemplate(Guid id);
    Task DeleteTemplate(Guid id);
}