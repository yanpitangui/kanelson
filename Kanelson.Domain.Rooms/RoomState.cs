using Kanelson.Domain.Rooms.Models;
using Kanelson.Domain.Templates.Models;
using Kanelson.Domain.Users;
using MessagePack;

namespace Kanelson.Domain.Rooms;

[MessagePackObject]
public sealed record RoomState
{
    [Key(0)]
    public string OwnerId { get; set; } = null!;

    [Key(1)]
    public string Name { get; set; } = null!;

    [Key(2)]
    public Dictionary<Guid, Dictionary<string, RoomAnswer>> Answers { get; init; } = new();

    [Key(3)]
    public Template Template { get; set; } = null!;

    [Key(4)]
    public RoomStatus CurrentState { get; set; } = RoomStatus.Created;

    [Key(5)]
    public HashSet<RoomUser> CurrentUsers { get; set; } = new();

    [Key(6)]
    public int CurrentQuestionIdx { get; set; }

    [Key(7)]
    public int MaxQuestionIdx { get; set; }
}

[MessagePackObject]
public record RoomAnswer
{
    [Key(0)]
    public IEnumerable<Guid> Alternatives { get; init; } = Enumerable.Empty<Guid>();

    [Key(1)]
    public required TimeSpan TimeToAnswer { get; init; } = new();

    [Key(2)]
    public required decimal Points { get; set; }
}

[MessagePackObject]
public record UserRanking : UserInfo
{
    [Key(2)]
    public decimal Points { get; set; }

    [Key(3)]
    public decimal AverageTime { get; set; }

    [Key(4)]
    public int? Rank { get; set; }
}

[MessagePackObject]
public record RoomUser : UserInfo
{
    [Key(2)]
    public bool Owner { get; set; }

    [Key(3)]
    public bool Answered { get; set; }
}
