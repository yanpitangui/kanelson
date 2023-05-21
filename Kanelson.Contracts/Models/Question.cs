namespace Kanelson.Contracts.Models;

public record QuestionSummary
{
    public Guid Id { get; init; }
    
    public string Name { get; init; } = null!;
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
    
    public List<Answer> Answers { get; init; } = new();
    
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
    
    public string Description { get; set; } = null!;
    
    public bool Correct { get; set; }
}

public enum QuestionType
{
    TrueFalse,
    Quiz
}