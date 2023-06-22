namespace Kanelson.Hubs;

public static class SignalRMessages
{
    public const string CurrentUsersUpdated = nameof(CurrentUsersUpdated);
    public const string RoomStatusChanged = nameof(RoomStatusChanged);
    public const string NextQuestion = nameof(NextQuestion);
    public const string RoundFinished = nameof(RoundFinished);
    public const string Answer = nameof(Answer);
    public const string JoinRoom = nameof(JoinRoom);
    public const string UserAnswered = nameof(UserAnswered);
    public const string RoomDeleted = nameof(RoomDeleted);
    public const string RoomFinished = nameof(RoomFinished);
    public const string RoundSummary = nameof(RoundSummary);
    public const string AnswerRejected = nameof(AnswerRejected);
}