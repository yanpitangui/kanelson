namespace Kanelson.Actors.Questions;

public record QuestionIndexState
{
    public HashSet<string> Indexes = new();
}