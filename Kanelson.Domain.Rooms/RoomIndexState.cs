using MessagePack;

namespace Kanelson.Domain.Rooms;

[MessagePackObject]
public class RoomIndexState
{
    [Key(0)]
    public Dictionary<string, BasicRoomInfo> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

[MessagePackObject]
public record BasicRoomInfo(
    [property: Key(0)] string Id,
    [property: Key(1)] string Name,
    [property: Key(2)] string OwnerId);
