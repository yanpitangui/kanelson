using Kanelson.Contracts.Models;

namespace Kanelson.Grains.Questions;

public record UserQuestionsState
{
    public Dictionary<Guid, Question> Questions { get; set; } = new();
}