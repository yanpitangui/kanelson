namespace Kanelson.Domain.Rooms;

public class RoomIndexState
{
    public Dictionary<string, BasicRoomInfo> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public record BasicRoomInfo(string Id, string Name, string OwnerId);