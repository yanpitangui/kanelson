using Kanelson.Domain.Templates.Models;
using MessagePack;

namespace Kanelson.Domain.Templates;

public static class RoomTemplateCommands
{
    [MessagePackObject]
    public sealed record Upsert([property: Key(0)] Template Template);

    [MessagePackObject]
    internal sealed record Register([property: Key(0)] Guid Id);
}
