using MessagePack;

namespace Kanelson.Domain.Templates;

[MessagePackObject]
public class RoomTemplateIndexState
{
    [Key(0)]
    public HashSet<Guid> Items { get; set; } = new();
}
