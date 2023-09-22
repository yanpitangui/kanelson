namespace Kanelson.Common;

public record ShutdownCommand
{
    public static readonly ShutdownCommand Instance = new();
}