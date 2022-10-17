namespace Shared.Models;


public record Template
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    
    public ImmutableArray<TemplateQuestion> Questions { get; set; } = ImmutableArray<TemplateQuestion>.Empty;
}

public record TemplateSummary(Guid Id, string Name){ }

public record TemplateQuestion : Question
{
    public int Order { get; init; } = 0;
}