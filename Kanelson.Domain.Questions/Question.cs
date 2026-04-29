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

    [Url(ErrorMessage = "ValidationUrl")]
    [MsgKey(1)]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "ValidationRequired")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "ValidationStringLength")]
    [MsgKey(2)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Time limit in seconds for someone to answer it
    /// </summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [MsgKey(3)]
    public int TimeLimit { get; set; } = 10;

    [Required(ErrorMessage = "ValidationRequired")]
    [Range(0, 2000, ErrorMessage = "ValidationRange")]
    [MsgKey(4)]
    public int Points { get; set; } = 1000;

    [Required(ErrorMessage = "ValidationRequired")]
    [ValidateComplexType]
    [MsgKey(5)]
    public List<Alternative> Alternatives { get; init; } = new(2);

    [Required(ErrorMessage = "ValidationRequired")]
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

    [Required(ErrorMessage = "ValidationRequired")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "ValidationStringLength")]
    [MsgKey(1)]
    public string Description { get; set; } = null!;

    [Required(ErrorMessage = "ValidationRequired")]
    [MsgKey(2)]
    public bool Correct { get; set; }
}

public enum QuestionType
{
    TrueFalse,
    Quiz,
    MultiCorrect
}
