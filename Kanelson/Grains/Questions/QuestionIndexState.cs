namespace Kanelson.Grains.Questions;

public record QuestionIndexState
{
    public HashSet<string> Indexes = new();
}