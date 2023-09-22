using Kanelson.Common;

namespace Kanelson.Domain.Users;

public static class UserCommands
{
    public sealed record UpsertUser(string UserId, string Name) : IWithUserId;

}