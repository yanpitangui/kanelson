using Kanelson.Domain.Questions;
using System.ComponentModel.DataAnnotations;

namespace Kanelson.Domain.Templates.Models;


public record Template
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Required]
    [StringLength(30, MinimumLength = 3)]
    public string Name { get; set; } = null!;
    
    public List<TemplateQuestion> Questions { get; set; } = new();
}

public record TemplateSummary(Guid Id, string Name){ }



public record TemplateQuestion : Question
{
    public int Order { get; init; } = 0;
}