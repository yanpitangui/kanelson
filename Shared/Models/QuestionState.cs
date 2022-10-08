namespace Shared.Models;

[Serializable]
public record QuestionState
{
    public Dictionary<Guid, Question> Questions { get; set; } = new();
}

public record QuestionSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public record Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;

    /// <summary>
    /// Time limit in seconds for someone to answer it
    /// </summary>
    public int TimeLimit { get; set; } = 5;

    public int Points { get; set; } = 1000;
    
    public List<Answer> Answers { get; set; } = new();
    
    public QuestionType Type { get; set; }
}

public class AnswerComparer : IEqualityComparer<Answer>
{
    public bool Equals(Answer? a, Answer? b) => a?.Id == b?.Id;
    public int GetHashCode(Answer x) => HashCode.Combine(x?.Id);
}

public record Answer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Descricao { get; set; } = null!;
    
    public bool Correct { get; set; }
}

public enum QuestionType
{
    TrueFalse,
    Quiz
}

public static class QuestionTypeExtensions
{
    public static string GetDescription(this QuestionType type)
    {
        return type switch
        {
            QuestionType.Quiz => "Quiz",
            QuestionType.TrueFalse => "Verdadeiro ou falso",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}