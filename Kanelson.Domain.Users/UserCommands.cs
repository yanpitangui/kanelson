using Kanelson.Common;
using MessagePack;

namespace Kanelson.Domain.Users;

public static class UserCommands
{
    [MessagePackObject]
    public sealed record UpsertUser([property: Key(0)] string UserId, [property: Key(1)] string Name) : IWithUserId;
}
