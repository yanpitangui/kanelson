using MessagePack;

namespace Kanelson.Domain.Users;

[MessagePackObject]
public record UserInfo
{
    public UserInfo()
    {
    }

    public UserInfo(string id)
    {
        Id = id;
    }

    [Key(0)]
    public string Id { get; init; } = null!;

    [Key(1)]
    public string Name { get; set; } = null!;
}