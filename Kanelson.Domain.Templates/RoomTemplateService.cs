using Akka.Actor;
using Akka.Hosting;
using Kanelson.Domain.Templates.Models;
using Kanelson.Domain.Users;
using System.Collections.Immutable;

namespace Kanelson.Domain.Templates;

public class RoomTemplateService : IRoomTemplateService
{
    private readonly IUserService _userService;
    private readonly IActorRef _shardRegion;
    

    public RoomTemplateService(ActorRegistry actorRegistry, IUserService userService)
    {
        _userService = userService;
        _shardRegion = actorRegistry.Get<RoomTemplateIndex>();

    }

    public async Task UpsertTemplate(Template template)
    {
        var actor = await _shardRegion.Ask<IActorRef>(new RoomTemplateQueries.GetRef(_userService.CurrentUser, template.Id), TimeSpan.FromSeconds(3));
        actor.Tell(new RoomTemplateCommands.Upsert(template));
    }

    public Task<ImmutableArray<TemplateSummary>> GetTemplates()
    {
        return _shardRegion.Ask<ImmutableArray<TemplateSummary>>(new RoomTemplateQueries.GetAllSummaries(_userService.CurrentUser), TimeSpan.FromSeconds(3));
    }

    public async Task<Template> GetTemplate(Guid id)
    {
        var exists = await _shardRegion.Ask<bool>(new RoomTemplateQueries.Exists(_userService.CurrentUser, id), TimeSpan.FromSeconds(3));
        if (!exists)
        {
            throw new KeyNotFoundException();
        }

        var actorRef = await _shardRegion.Ask<IActorRef>(new RoomTemplateQueries.GetRef(_userService.CurrentUser, id), TimeSpan.FromSeconds(3));
        return await actorRef.Ask<Template>(RoomTemplateQueries.GetTemplate.Instance, TimeSpan.FromSeconds(3));
    }

    public void DeleteTemplate(Guid id)
    {
        _shardRegion.Tell(new RoomTemplateQueries.Unregister(_userService.CurrentUser, id));
    }
}