using Kanelson.Contracts.Models;

namespace Kanelson.Actors.Rooms;

public record RoomState
{
    public string OwnerId { get; set; } = null!;
    
    public string Name { get; set; } = null!;

    public Dictionary<Guid, Dictionary<string, RoomAnswer>> Answers { get; init; } = new();

    public Template Template { get; set; } = null!;
    
    public RoomStatus CurrentState { get; set; } = RoomStatus.Created;

    public HashSet<RoomUser> CurrentUsers { get; set; } = new();
    
    public int CurrentQuestionIdx { get; set; }
    
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
    
    public int? Rank { get; set; }
}

public record RoomUser : UserInfo
{
    public bool Owner { get; set; }
}
