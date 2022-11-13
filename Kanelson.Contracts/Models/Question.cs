namespace Kanelson.Contracts.Models;

[GenerateSerializer]
[Immutable]
public record QuestionSummary
{
    [Id(0)]
    public Guid Id { get; init; }
    
    [Id(1)]
    public string Name { get; init; } = null!;
}

[GenerateSerializer]
public record Question
{
    [Id(0)]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Id(1)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Time limit in seconds for someone to answer it
    /// </summary>
    [Id(2)]
    public int TimeLimit { get; set; } = 5;

    [Id(3)]
    public int Points { get; set; } = 1000;
    
    [Id(4)]
    public List<Answer> Answers { get; init; } = new()
    {
        new Answer
        {
            Correct = true,
            Description = "Verdadeiro"
        },
        new Answer
        {
            Correct = false,
            Description = "Falso"
        }
    };
    
    [Id(5)]
    public QuestionType Type { get; set; }
}

public class AnswerComparer : IEqualityComparer<Answer>
{
    public bool Equals(Answer? a, Answer? b) => a?.Id == b?.Id;
    public int GetHashCode(Answer x) => HashCode.Combine(x?.Id);
}

[GenerateSerializer]
public record Answer
{
    [Id(0)]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Id(1)]
    public string Description { get; set; } = null!;
    
    [Id(2)]
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