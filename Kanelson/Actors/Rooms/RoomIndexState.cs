namespace Kanelson.Actors.Rooms;

public class RoomIndexState
{
    public HashSet<string> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}