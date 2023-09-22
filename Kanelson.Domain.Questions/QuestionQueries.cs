using Kanelson.Common;

namespace Kanelson.Domain.Questions;

public static class QuestionQueries
{
    public sealed record GetQuestions(string UserId, params Guid[] Ids): IWithUserId;

    public sealed record GetQuestion(string UserId, Guid Id) : IWithUserId;

    public sealed record GetQuestionsSummary(string UserId) : IWithUserId;
}