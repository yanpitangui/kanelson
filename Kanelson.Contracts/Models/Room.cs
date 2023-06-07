namespace Kanelson.Contracts.Models;

public record RoomSummary(long Id, string Name, UserInfo Owner, RoomStatus Status);

public enum RoomStatus
{
    Created,
    Started,
    DisplayingQuestion,
    AwaitingForNextQuestion,
    Finished,
    Abandoned
}

public record CurrentQuestionInfo(Question Question, int CurrentNumber, int MaxNumber);