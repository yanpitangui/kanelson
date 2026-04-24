using Kanelson.Domain.Questions;
using Kanelson.Domain.Users;
using MessagePack;

namespace Kanelson.Domain.Rooms.Models;

[MessagePackObject]
public record RoomSummary([property: Key(0)] string Id, [property: Key(1)] string Name, [property: Key(2)] UserInfo Owner);

public enum RoomStatus
{
    Created,
    Started,
    DisplayingQuestion,
    AwaitingForNextQuestion,
    Finished,
    Abandoned
}

[MessagePackObject]
public record CurrentQuestionInfo(
    [property: Key(0)] Question Question,
    [property: Key(1)] int CurrentNumber,
    [property: Key(2)] int MaxNumber);

[MessagePackObject]
public record UserAnswerSummary(
    [property: Key(0)] Question Question,
    [property: Key(1)] IEnumerable<Guid> Answered);
