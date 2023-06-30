using System.Collections.Immutable;
using Kanelson.Models;

namespace Kanelson.Services;

public interface IUserService
{
    string CurrentUser { get; }
    
    public void Upsert(string id, string name);

    public Task<ImmutableArray<UserInfo>> GetUsersInfo(params string[] ids);
    Task<UserInfo> GetUserInfo(string id);
}