using System.Collections.Immutable;
using Orleans;
using Orleans.Runtime;
using Shared.Grains;
using Shared.Models;

namespace Kanelson.Grains;

public class UserManagerGrain : Grain, IUserManagerGrain
{
    private readonly IPersistentState<UsersState> _state;

    public UserManagerGrain([PersistentState("users", "kanelson-storage")]
        IPersistentState<UsersState> users)
    {
        _state = users;
    }
    
    public async Task Upsert(string id, string name)
    {
        var users = _state.State.Users;
        users.RemoveWhere(x => x.Id == id);
        _state.State.Users.Add(new UserInfo(id, name));
        await _state.WriteStateAsync();
    }

    public Task<ImmutableArray<UserInfo>> GetUsersInfo(params string[] ids)
    {
        return Task.FromResult(
            _state.State.Users.Where(x => ids.Contains(x.Id)).ToImmutableArray()
            );
    }

    public Task<UserInfo> GetUserInfo(string id)
    {
        return Task.FromResult(_state.State.Users.First(x => x.Id == id));
    }
}

[Serializable]
public record UsersState
{
    public HashSet<UserInfo> Users { get; set; } = new();
}