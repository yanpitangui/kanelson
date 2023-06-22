namespace Kanelson.Actors;

public record ShutdownCommand
{
    public static readonly ShutdownCommand Instance = new();
}