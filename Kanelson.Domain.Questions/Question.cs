using System.ComponentModel.DataAnnotations;
using MessagePack;
using MsgKey = MessagePack.KeyAttribute;

namespace Kanelson.Domain.Questions;

[MessagePackObject]
public record QuestionSummary
{
    [MsgKey(0)]
    public Guid Id { get; init; }

    [MsgKey(1)]
    public string Name { get; init; } = null!;
}

[MessagePackObject]
public record Question
{
    [MsgKey(0)]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Url]
    [MsgKey(1)]
    public string? ImageUrl { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 3)]
    [MsgKey(2)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Time limit in seconds for someone to answer it
    /// </summary>
    [Required]
    [MsgKey(3)]
    public int TimeLimit { get; set; } = 10;

    [Required]
    [Range(0, 2000)]
    [MsgKey(4)]
    public int Points { get; set; } = 1000;

    [Required]
    [ValidateComplexType]
    [MsgKey(5)]
    public List<Alternative> Alternatives { get; init; } = new(2);

    [Required]
    [MsgKey(6)]
    public QuestionType Type { get; set; }
}

public class AlternativeComparer : IEqualityComparer<Alternative>
{
    public bool Equals(Alternative? a, Alternative? b) => a?.Id == b?.Id;
    public int GetHashCode(Alternative x) => HashCode.Combine(x?.Id);
}

[MessagePackObject]
public record Alternative
{
    [MsgKey(0)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(200, MinimumLength = 1)]
    [MsgKey(1)]
    public string Description { get; set; } = null!;

    [Required]
    [MsgKey(2)]
    public bool Correct { get; set; }
}

public enum QuestionType
{
    TrueFalse,
    Quiz,
    MultiCorrect
}
