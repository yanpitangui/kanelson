using Kanelson.Common;
using MessagePack;

namespace Kanelson.Domain.Users;

public static class UserHistoryQueries
{
    [MessagePackObject]
    public sealed record GetHistory([property: Key(0)] string UserId) : IWithUserId;
}
