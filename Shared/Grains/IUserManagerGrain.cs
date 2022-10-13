using Orleans;
using Shared.Models;

namespace Shared.Grains;

public interface IUserManagerGrain : IGrainWithIntegerKey
{
    public Task Upsert(string id, string name);

    public Task<List<UserInfo>> GetUserInfo(params string[] ids);
}