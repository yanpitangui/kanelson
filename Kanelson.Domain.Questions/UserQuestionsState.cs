using MessagePack;

namespace Kanelson.Domain.Questions;

[MessagePackObject]
public record UserQuestionsState
{
    [Key(0)]
    public Dictionary<Guid, Question> Questions { get; set; } = new();
}
