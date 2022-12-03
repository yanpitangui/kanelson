using System.Collections.Concurrent;
using Kanelson.Contracts.Models;

namespace Kanelson.Grains.Rooms;

[GenerateSerializer]
public record RoomState
{
    [Id(0)]
    public string OwnerId { get; set; } = null!;
    
    [Id(1)]
    public string Name { get; set; } = null!;

    [Id(2)]
    public ConcurrentDictionary<Guid, ConcurrentDictionary<string, RoomAnswer>> Answers { get; init; } = new();

    [Id(3)]
    public Template Template { get; set; } = null!;
    
    [Id(4)]
    public RoomStatus CurrentState { get; set; } = RoomStatus.Created;

    [Id(5)]
    public HashSet<UserInfo> CurrentUsers { get; set; } = new();
    
    [Id(6)]
    public int CurrentQuestionIdx { get; set; }
    
    [Id(7)]
    public int MaxQuestionIdx { get; set; }
}


public record RoomAnswer
{
    public Guid AnswerId { get; init; }

    public TimeSpan TimeToAnswer { get; init; } = new();
    
    public decimal Points { get; init; }
    public bool Correct { get; set; }
}

public record UserRanking : UserInfo
{
    public decimal Points { get; set; }
    
    public decimal AverageTime { get; set; }
    
    public int Rank { get; set; }
}
