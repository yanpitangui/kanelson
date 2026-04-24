using Kanelson.Common;
using MessagePack;

namespace Kanelson.Domain.Templates;

public static class RoomTemplateQueries
{
    [MessagePackObject]
    public sealed record GetSummary
    {
        public GetSummary() { }
        public static GetSummary Instance { get; } = new();
    }

    [MessagePackObject]
    public sealed record GetTemplate
    {
        public GetTemplate() { }
        public static GetTemplate Instance { get; } = new();
    }

    [MessagePackObject]
    public sealed record GetAllSummaries([property: Key(0)] string UserId) : IWithUserId;

    [MessagePackObject]
    public sealed record Exists([property: Key(0)] string UserId, [property: Key(1)] Guid Id) : IWithUserId;

    [MessagePackObject]
    public sealed record GetRef([property: Key(0)] string UserId, [property: Key(1)] Guid Id) : IWithUserId;

    [MessagePackObject]
    public sealed record Unregister([property: Key(0)] string UserId, [property: Key(1)] Guid Id) : IWithUserId;
}
