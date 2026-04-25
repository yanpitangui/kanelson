using MessagePack;

namespace Kanelson.Common;

[MessagePackObject]
public record ShutdownCommand
{
    public static readonly ShutdownCommand Instance = new();
}
