using Kanelson.Common;

namespace Kanelson.Domain.Users;

public static class UserQueries
{
    public sealed record GetUserInfo(string UserId) : IWithUserId;
}