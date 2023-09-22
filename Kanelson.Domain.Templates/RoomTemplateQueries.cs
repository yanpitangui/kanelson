using Kanelson.Common;

namespace Kanelson.Domain.Templates;

public static class RoomTemplateQueries
{
    public sealed record GetSummary
    {
        private GetSummary()
        {
        }

        public static GetSummary Instance { get; } = new();
    }

    public sealed record GetTemplate
    {
        private GetTemplate()
        {
        }

        public static GetTemplate Instance { get; } = new();
    }
    
    public sealed record GetAllSummaries(string UserId) : IWithUserId;

    public sealed record Exists(string UserId, Guid Id) : IWithUserId;

    public sealed record GetRef(string UserId, Guid Id) : IWithUserId;

    public sealed record Unregister(string UserId, Guid Id) : IWithUserId;


}