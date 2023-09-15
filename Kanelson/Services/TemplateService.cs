using System.Collections.Immutable;
using Akka.Actor;
using Akka.Hosting;
using Kanelson.Actors.Templates;
using Kanelson.Models;
using Template = Kanelson.Models.Template;

namespace Kanelson.Services;

public class TemplateService : ITemplateService
{
    private readonly IUserService _userService;
    private readonly IActorRef _shardRegion;
    

    public TemplateService(ActorRegistry actorRegistry, IUserService userService)
    {
        _userService = userService;
        _shardRegion = actorRegistry.Get<TemplateIndex>();

    }

    public async Task UpsertTemplate(Template template)
    {
        var actor = await _shardRegion.Ask<IActorRef>(new TemplateQueries.GetRef(_userService.CurrentUser, template.Id), TimeSpan.FromSeconds(3));
        actor.Tell(new TemplateCommands.Upsert(template));
    }

    public Task<ImmutableArray<TemplateSummary>> GetTemplates()
    {
        return _shardRegion.Ask<ImmutableArray<TemplateSummary>>(new TemplateQueries.GetAllSummaries(_userService.CurrentUser), TimeSpan.FromSeconds(3));
    }

    public async Task<Template> GetTemplate(Guid id)
    {
        var exists = await _shardRegion.Ask<bool>(new TemplateQueries.Exists(_userService.CurrentUser, id), TimeSpan.FromSeconds(3));
        if (!exists)
        {
            throw new KeyNotFoundException();
        }

        var actorRef = await _shardRegion.Ask<IActorRef>(new TemplateQueries.GetRef(_userService.CurrentUser, id), TimeSpan.FromSeconds(3));
        return await actorRef.Ask<Template>(TemplateQueries.GetTemplate.Instance, TimeSpan.FromSeconds(3));
    }

    public void DeleteTemplate(Guid id)
    {
        _shardRegion.Tell(new TemplateQueries.Unregister(_userService.CurrentUser, id));
    }
}