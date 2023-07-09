using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Kanelson.Actors.Templates;
using Kanelson.Models;
using Template = Kanelson.Models.Template;

namespace Kanelson.Services;

public class TemplateService : ITemplateService
{
    private readonly IUserService _userService;
    private readonly IActorRef _index;
    

    public TemplateService(ActorRegistry actorRegistry, IUserService userService)
    {
        _userService = userService;
        _index = actorRegistry.Get<TemplateIndex>();

    }

    public async Task UpsertTemplate(Template template)
    {
        var actor = await _index.Ask<IActorRef>(MessageEnvelope(new GetRef(template.Id)), TimeSpan.FromSeconds(3));
        actor.Tell(MessageEnvelope(new Upsert(template, _userService.CurrentUser)));
    }

    public Task<ImmutableArray<TemplateSummary>> GetTemplates()
    {
        return _index.Ask<ImmutableArray<TemplateSummary>>(MessageEnvelope(GetAllSummaries.Instance), TimeSpan.FromSeconds(3));
    }

    public async Task<Template> GetTemplate(Guid id)
    {
        var exists = await _index.Ask<bool>(MessageEnvelope(new Exists(id)), TimeSpan.FromSeconds(3));
        if (!exists)
        {
            throw new KeyNotFoundException();
        }

        var actorRef = await _index.Ask<IActorRef>(MessageEnvelope(new GetRef(id)), TimeSpan.FromSeconds(3));
        return await actorRef.Ask<Template>(MessageEnvelope(Actors.Templates.GetTemplate.Instance), TimeSpan.FromSeconds(3));
    }

    public async Task DeleteTemplate(Guid id)
    {
        var exists = await _index.Ask<bool>(MessageEnvelope(new Exists(id)), TimeSpan.FromSeconds(3));
        if (!exists)
        {
            throw new KeyNotFoundException();
        }
        _index.Tell(MessageEnvelope(new Unregister(id)));
    }

    private ShardingEnvelope MessageEnvelope<T>(T message) where T: class
    {
        return new ShardingEnvelope(_userService.CurrentUser, message);
    }
}