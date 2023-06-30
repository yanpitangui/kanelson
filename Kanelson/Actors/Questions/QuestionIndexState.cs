namespace Kanelson.Actors.Questions;

public record QuestionIndexState
{
    public HashSet<string> Index { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}