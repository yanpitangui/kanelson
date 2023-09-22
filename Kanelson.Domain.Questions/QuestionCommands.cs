using Kanelson.Common;

namespace Kanelson.Domain.Questions;

public static class QuestionCommands
{
    public sealed record UpsertQuestion(string UserId, Question Question) : IWithUserId;

    public sealed record RemoveQuestion(string UserId, Guid Id): IWithUserId;
}