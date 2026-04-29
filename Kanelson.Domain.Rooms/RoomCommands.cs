using Kanelson.Domain.Templates.Models;
using Kanelson.Domain.Users;
using MessagePack;

namespace Kanelson.Domain.Rooms;

public static class RoomCommands
{
    [MessagePackObject]
    public sealed record UserConnected(
        [property: Key(0)] string RoomId,
        [property: Key(1)] string UserId) : IWithRoomId;

    [MessagePackObject]
    public sealed record SetBase(
        [property: Key(0)] string RoomId,
        [property: Key(1)] string RoomName,
        [property: Key(2)] string OwnerId,
        [property: Key(3)] Template Template) : IWithRoomId;

    [MessagePackObject]
    public sealed record UpdateCurrentUsers(
        [property: Key(0)] string RoomId,
        [property: Key(1)] HashSet<UserInfo> Users) : IWithRoomId;

    [MessagePackObject]
    public sealed record Start([property: Key(0)] string RoomId) : IWithRoomId;

    [MessagePackObject]
    public sealed record NextQuestion([property: Key(0)] string RoomId) : IWithRoomId;

    [MessagePackObject]
    public sealed record Shutdown([property: Key(0)] string RoomId) : IWithRoomId;

    [MessagePackObject]
    public sealed record SendUserAnswer(
        [property: Key(0)] string RoomId,
        [property: Key(1)] string UserId,
        [property: Key(2)] Guid[] AlternativeIds) : IWithRoomId;

    [MessagePackObject]
    public sealed record ExtendTime(
        [property: Key(0)] string RoomId,
        [property: Key(1)] int Seconds) : IWithRoomId;
}
