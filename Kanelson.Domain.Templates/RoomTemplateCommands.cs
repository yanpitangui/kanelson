using Kanelson.Domain.Templates.Models;

namespace Kanelson.Domain.Templates;

public static class RoomTemplateCommands
{
    public sealed record Upsert(Template Template);

    internal sealed record Register(Guid Id);
}