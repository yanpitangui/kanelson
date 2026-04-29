using System.ComponentModel.DataAnnotations;
using Kanelson.Common;
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

    [Url(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ValidationUrl")]
    [MsgKey(1)]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ValidationRequired")]
    [StringLength(200, MinimumLength = 3, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ValidationStringLength")]
    [MsgKey(2)]
    public string Name { get; set; } = null!;

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ValidationRequired")]
    [MsgKey(3)]
    public int TimeLimit { get; set; } = 10;

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ValidationRequired")]
    [Range(0, 2000, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ValidationRange")]
    [MsgKey(4)]
    public int Points { get; set; } = 1000;

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ValidationRequired")]
    [ValidateComplexType]
    [MsgKey(5)]
    public List<Alternative> Alternatives { get; init; } = new(2);

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ValidationRequired")]
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

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ValidationRequired")]
    [StringLength(200, MinimumLength = 1, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ValidationStringLength")]
    [MsgKey(1)]
    public string Description { get; set; } = null!;

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ValidationRequired")]
    [MsgKey(2)]
    public bool Correct { get; set; }
}

public enum QuestionType
{
    TrueFalse,
    Quiz,
    MultiCorrect
}
