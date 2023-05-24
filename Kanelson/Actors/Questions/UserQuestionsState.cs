using Kanelson.Contracts.Models;

namespace Kanelson.Actors.Questions;

public record UserQuestionsState
{
    public Dictionary<Guid, Question> Questions { get; set; } = new();
}