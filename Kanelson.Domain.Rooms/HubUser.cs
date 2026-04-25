using Kanelson.Domain.Users;
using MessagePack;

namespace Kanelson.Domain.Rooms;

[MessagePackObject]
public record HubUser : UserInfo
{
    public static HubUser FromUserInfo(UserInfo userInfo) => new() { Id = userInfo.Id, Name = userInfo.Name };

    [Key(2)]
    public HashSet<string> Connections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
