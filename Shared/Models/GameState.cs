﻿namespace Shared.Models;

[Serializable]
public record GameState
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