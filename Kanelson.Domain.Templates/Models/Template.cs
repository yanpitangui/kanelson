using Kanelson.Domain.Questions;
using MessagePack;
using System.ComponentModel.DataAnnotations;
using MsgKey = MessagePack.KeyAttribute;

namespace Kanelson.Domain.Templates.Models;

[MessagePackObject]
public record Template
{
    [MsgKey(0)]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    [StringLength(30, MinimumLength = 3)]
    [MsgKey(1)]
    public string Name { get; set; } = null!;

    [MsgKey(2)]
    public List<TemplateQuestion> Questions { get; set; } = new();
}

[MessagePackObject]
public record TemplateSummary([property: MsgKey(0)] Guid Id, [property: MsgKey(1)] string Name);

[MessagePackObject]
public record TemplateQuestion : Question
{
    [MsgKey(7)]
    public int Order { get; init; } = 0;
}
