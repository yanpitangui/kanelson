using Kanelson.Common;
using MessagePack;

namespace Kanelson.Domain.Users;

public static class UserQueries
{
    [MessagePackObject]
    public sealed record GetUserInfo([property: Key(0)] string UserId) : IWithUserId;
}
