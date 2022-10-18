using Orleans;
using Shared.Models;

namespace Shared.Grains;

public interface IUserManagerGrain : IGrainWithIntegerKey
{
    public Task Upsert(string id, string name);

    public Task<ImmutableArray<UserInfo>> GetUsersInfo(params string[] ids);
    Task<UserInfo> GetUserInfo(string id);
}