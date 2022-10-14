namespace Shared.Models;


public record Game
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    
    public ImmutableArray<Question> Questions { get; init; } = ImmutableArray<Question>.Empty;
}

public record GameSummary(Guid Id, string Name){ }

public record UserAnswer
{
    public Guid QuestionId { get; init; }
    public Guid AnswerId { get; init; }
}


public enum GameStatus
{
    Created,
    Started,
    Finished,
    Abandoned
}