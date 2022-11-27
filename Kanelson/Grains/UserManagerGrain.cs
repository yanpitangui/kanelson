using System.Collections.Immutable;
using Orleans.Runtime;
using Kanelson.Contracts.Grains;
using Kanelson.Contracts.Models;

namespace Kanelson.Grains;

public class UserManagerGrain : IUserManagerGrain
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

[GenerateSerializer]
public record UsersState
{
    [Id(0)]
    public HashSet<UserInfo> Users { get; set; } = new();
}