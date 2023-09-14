namespace Kanelson.Actors.Users;

public static class UserCommands
{
    public sealed record UpsertUser(string UserId, string Name) : IWithUserId;

}