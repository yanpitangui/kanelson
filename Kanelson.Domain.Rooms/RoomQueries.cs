using MessagePack;

namespace Kanelson.Domain.Rooms;

public class RoomQueries
{
    [MessagePackObject]
    public record Exists([property: Key(0)] string RoomId) : IWithRoomId;

    [MessagePackObject]
    public sealed record GetCurrentState([property: Key(0)] string RoomId) : IWithRoomId;

    [MessagePackObject]
    public sealed record GetSummary([property: Key(0)] string RoomId) : IWithRoomId;

    [MessagePackObject]
    public sealed record GetCurrentQuestion([property: Key(0)] string RoomId) : IWithRoomId;

    [MessagePackObject]
    public sealed record GetOwner([property: Key(0)] string RoomId) : IWithRoomId;
}
