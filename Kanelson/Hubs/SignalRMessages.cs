namespace Kanelson.Hubs;

public static class SignalRMessages
{
    public const string CurrentUsersUpdated = nameof(CurrentUsersUpdated);
    public const string RoomStateChanged = nameof(RoomStateChanged);
    public const string NextQuestion = nameof(NextQuestion);
    public const string Start = nameof(Start);
    public const string RoundFinished = nameof(RoundFinished);
    public const string Answer = nameof(Answer);
    public const string JoinRoom = nameof(JoinRoom);
}