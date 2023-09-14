namespace Kanelson.Actors.Users;

public static class UserQueries
{
    public sealed record GetUserInfo(string UserId) : IWithUserId;
}