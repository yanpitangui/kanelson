using System.ComponentModel.DataAnnotations;

namespace Kanelson.Models;

public record QuestionSummary
{
    public Guid Id { get; init; }
    
    public string Name { get; init; } = null!;
}

public record Question
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Url]
    public string? ImageUrl { get; set; }
    
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Time limit in seconds for someone to answer it
    /// </summary>
    [Required]
    public int TimeLimit { get; set; } = 5;

    [Required]
    [Range(0, 2000)]
    public int Points { get; set; } = 1000;
    
    [Required]
    [ValidateComplexType]
    public List<Alternative> Alternatives { get; init; } = new(2);
    
    [Required]
    public QuestionType Type { get; set; }
}

public class AlternativeComparer : IEqualityComparer<Alternative>
{
    public bool Equals(Alternative? a, Alternative? b) => a?.Id == b?.Id;
    public int GetHashCode(Alternative x) => HashCode.Combine(x?.Id);
}

public record Alternative
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(200, MinimumLength = 4)]
    public string Description { get; set; } = null!;
    
    [Required]
    public bool Correct { get; set; }
}

public enum QuestionType
{
    TrueFalse,
    Quiz
}