using Kanelson.Common;
using MessagePack;

namespace Kanelson.Domain.Questions;

public static class QuestionCommands
{
    [MessagePackObject]
    public sealed record UpsertQuestion([property: Key(0)] string UserId, [property: Key(1)] Question Question) : IWithUserId;

    [MessagePackObject]
    public sealed record RemoveQuestion([property: Key(0)] string UserId, [property: Key(1)] Guid Id) : IWithUserId;
}
