namespace Kanelson.Contracts.Models;

public record RoomSummary(string Id, string Name, UserInfo Owner, RoomStatus Status);

public enum RoomStatus
{
    Created,
    Started,
    DisplayingQuestion,
    AwaitingForNextQuestion,
    Finished,
    Abandoned
}