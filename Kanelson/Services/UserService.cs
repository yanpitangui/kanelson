using System.Collections.Immutable;
using System.Security.Claims;
using Akka.Actor;
using Akka.Cluster.Sharding;
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
        var actor = _actorRegistry.Get<User>();
        actor.Tell(new UpsertUser(id, name));
    }
    
    public async Task<UserInfo> GetUserInfo(string id, CancellationToken ct = default)
    {
        var actor = await _actorRegistry.GetAsync<User>(ct);
        return await actor.Ask<UserInfo>(new GetUserInfo(id), ct);
    }
    
}