namespace Kanelson.Contracts.Models;


public record Template
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public string Name { get; set; } = null!;
    
    public List<TemplateQuestion> Questions { get; set; } = new();
}

public record TemplateSummary(Guid Id, string Name){ }



public record TemplateQuestion : Question
{
    public int Order { get; init; } = 0;
}