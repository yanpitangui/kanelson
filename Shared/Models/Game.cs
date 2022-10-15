namespace Shared.Models;


public record Game
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    
    public ImmutableArray<GameQuestion> Questions { get; set; } = ImmutableArray<GameQuestion>.Empty;
}

public record GameSummary(Guid Id, string Name){ }

public record UserAnswer
{
    public Guid QuestionId { get; init; }
    public Guid AnswerId { get; init; }
}

public record GameQuestion : Question
{
    public int Order { get; set; } = 0;
}


public enum GameStatus
{
    Created,
    Started,
    Finished,
    Abandoned
}