namespace Shared.Models;

[Serializable]
public record GameState
{
    public string OwnerId { get; set; } = null!;
    public string Name { get; set; } = null!;

    public GameStatus Status { get; set; } = GameStatus.Created;

    public List<Question> Questions { get; set; } = new();

    public IDictionary<string, List<UserAnswer>> Answers { get; set; } = new Dictionary<string, List<UserAnswer>>();

}

public record UserAnswer
{
    public Guid QuestionId { get; set; }
    public Guid AnswerId { get; set; }
}


public enum GameStatus
{
    Created,
    Started,
    Finished,
    Abandoned
}