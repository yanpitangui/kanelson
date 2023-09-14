using Kanelson.Actors.Users;

namespace Kanelson.Actors.Templates;

public static class TemplateQueries
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