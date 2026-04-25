using Kanelson.Domain.Templates.Models;
using MessagePack;

namespace Kanelson.Domain.Templates;

[MessagePackObject]
public record RoomTemplateState
{
    [Key(0)]
    public Template Template { get; set; } = null!;
}
