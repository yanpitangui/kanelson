using Kanelson.Common;
using MessagePack;

namespace Kanelson.Domain.Users;

public static class UserHistoryCommands
{
    [MessagePackObject]
    public sealed record RecordPlacement(
        [property: Key(0)] string UserId,
        [property: Key(1)] RoomPlacement Placement) : IWithUserId;
}
