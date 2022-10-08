namespace Shared.Models;

[Serializable]
public record RoomState
{
    public string Name { get; set; } = null!;

    public State State { get; set; } = State.Created;
}


public enum State
{
    Created,
    Started,
    Finished,
    Abandoned
}