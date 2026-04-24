using MessagePack;

namespace Kanelson.Domain.Rooms;

public static class AllRoomsPublisherMessages
{
    [MessagePackObject]
    public sealed record RoomRegistered(
        [property: Key(0)] string RoomId,
        [property: Key(1)] string RoomName,
        [property: Key(2)] string OwnerId);

    [MessagePackObject]
    public sealed record RoomUnregistered([property: Key(0)] string RoomId);
}

public static class AllRoomsIndexMessages
{
    public sealed record GetRoomsReader
    {
        private GetRoomsReader() { }
        public static GetRoomsReader Instance { get; } = new();
    }

    public sealed record CheckRoomExists(string RoomId);
}
