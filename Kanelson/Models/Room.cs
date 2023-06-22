namespace Kanelson.Models;

public record RoomSummary(long Id, string Name, UserInfo Owner);

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

public record UserAnswerSummary(string AnswerName, List<Alternative> Alternatives, IEnumerable<Guid> Answered);