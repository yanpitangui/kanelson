namespace Kanelson.Contracts.Models;

[GenerateSerializer]
[Immutable]
public record RoomSummary(string Id, string Name, UserInfo Owner, RoomStatus Status);

public enum RoomStatus
{
    Created,
    Started,
    Finished,
    Abandoned
}