namespace Kanelson.Domain.Questions;

public record UserQuestionsState
{
    public Dictionary<Guid, Question> Questions { get; set; } = new();
}