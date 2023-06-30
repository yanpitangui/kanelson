using System.Collections.Immutable;
using System.Security.Claims;
using Akka.Actor;
using Akka.Hosting;
using Kanelson.Actors;
using Kanelson.Models;

namespace Kanelson.Services;

public class UserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ActorRegistry _actorRegistry;
    
    public UserService(IHttpContextAccessor httpContextAccessor, ActorRegistry actorRegistry)
    {
        _httpContextAccessor = httpContextAccessor;
        _actorRegistry = actorRegistry;
    }

    public string CurrentUser => _httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    
    public void Upsert(string id, string name)
    {
        var actor = _actorRegistry.Get<UserIndexActor>();
        actor.Tell(new UpsertUser(id, name));
    }

    public async Task<ImmutableArray<UserInfo>> GetUsersInfo(params string[] ids)
    {
        var actor = _actorRegistry.Get<UserIndexActor>();
        return await actor.Ask<ImmutableArray<UserInfo>>(new GetUserInfos(ids));
    }
    
    public async Task<UserInfo> GetUserInfo(string id)
    {
        var actor = _actorRegistry.Get<UserIndexActor>();
        var result = await actor.Ask<ImmutableArray<UserInfo>>(new GetUserInfos(id));
        return result.First();
    }
}