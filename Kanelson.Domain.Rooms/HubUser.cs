using Kanelson.Domain.Users;

namespace Kanelson.Domain.Rooms;

public record HubUser : UserInfo
{

    public static HubUser FromUserInfo(UserInfo userInfo) => new() {Id = userInfo.Id, Name = userInfo.Name};
    public HashSet<string> Connections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}