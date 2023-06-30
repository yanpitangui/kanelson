using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Actor;
using Kanelson.Actors.Templates;
using Kanelson.Models;

namespace Kanelson.Services;

public class TemplateService : ITemplateService
{
    private readonly ActorSystem _actorSystem;
    private readonly IUserService _userService;


    private static readonly ConcurrentDictionary<string, IActorRef> Indexes;

    static TemplateService()
    {
        Indexes = new ConcurrentDictionary<string, IActorRef>(StringComparer.OrdinalIgnoreCase);
    }

    public TemplateService(ActorSystem actorSystem, IUserService userService)
    {
        _actorSystem = actorSystem;
        _userService = userService;
    }

    public async Task UpsertTemplate(Template template)
    {
        var index = GetOrCreateIndexRef();
        var actor = await index.Ask<IActorRef>(new GetRef(template.Id));
        actor.Tell(new Upsert(template, _userService.CurrentUser));
    }

    public Task<ImmutableArray<TemplateSummary>> GetTemplates()
    {
        var index = GetOrCreateIndexRef();
        return index.Ask<ImmutableArray<TemplateSummary>>(GetAllSummaries.Instance);
    }

    public async Task<Template> GetTemplate(Guid id)
    {
        var index = GetOrCreateIndexRef();
        var exists = await index.Ask<bool>(new Exists(id));
        if (!exists)
        {
            throw new KeyNotFoundException();
        }

        var actorRef = await index.Ask<IActorRef>(new GetRef(id));
        return await actorRef.Ask<Template>(Actors.Templates.GetTemplate.Instance);
    }

    public async Task DeleteTemplate(Guid id)
    {
        var index = GetOrCreateIndexRef();
        var exists = await index.Ask<bool>(new Exists(id));
        if (!exists)
        {
            throw new KeyNotFoundException();
        }
        index.Tell(new Unregister(id));
    }

    private IActorRef GetOrCreateIndexRef()
    {
        var exists = Indexes.TryGetValue(_userService.CurrentUser, out var actorRef);
        if (Equals(actorRef, ActorRefs.Nobody) || !exists)
        {
            actorRef = _actorSystem.ActorOf(TemplateIndexActor.Props(_userService.CurrentUser), $"template-index-{_userService.CurrentUser}");
        }

        Indexes.AddOrUpdate(_userService.CurrentUser, _ => actorRef!, 
            (_, _) => actorRef!);

        return actorRef!;

    }
}