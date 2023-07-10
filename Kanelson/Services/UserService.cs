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
        actor.Tell(MessageEnvelope(id, new UpsertUser(name)));
    }
    
    public Task<UserInfo> GetUserInfo(string id)
    {
        var actor = _actorRegistry.Get<User>();
        return actor.Ask<UserInfo>(MessageEnvelope(id, Actors.GetUserInfo.Instance), TimeSpan.FromSeconds(3));
    }

    private ShardingEnvelope MessageEnvelope<T>(string id, T message) where T: class
    {
        return new ShardingEnvelope(id, message);
    }
}