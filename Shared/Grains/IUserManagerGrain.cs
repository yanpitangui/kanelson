using Shared.Models;

namespace Shared.Grains;

public interface IUserManagerGrain
{
    public Task Upsert(string Id, string Name);

    public Task<List<UserInfo>> GetUserInfo(params string[] ids);
}