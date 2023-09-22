namespace Kanelson.Domain.Users;

public interface IUserService
{
    string CurrentUser { get; }
    
    public void Upsert(string id, string name);

    Task<UserInfo> GetUserInfo(string id, CancellationToken ct = default);
}