using System.Collections.Immutable;
using Kanelson.Models;

namespace Kanelson.Services;

public interface IUserService
{
    string CurrentUser { get; }
    
    public void Upsert(string id, string name);

    Task<UserInfo> GetUserInfo(string id, CancellationToken ct = default);
}