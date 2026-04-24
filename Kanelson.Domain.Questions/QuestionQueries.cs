using Kanelson.Common;
using MessagePack;

namespace Kanelson.Domain.Questions;

public static class QuestionQueries
{
    [MessagePackObject]
    public sealed record GetQuestions([property: Key(0)] string UserId, [property: Key(1)] params Guid[] Ids) : IWithUserId;

    [MessagePackObject]
    public sealed record GetQuestion([property: Key(0)] string UserId, [property: Key(1)] Guid Id) : IWithUserId;

    [MessagePackObject]
    public sealed record GetQuestionsSummary([property: Key(0)] string UserId) : IWithUserId;
}
