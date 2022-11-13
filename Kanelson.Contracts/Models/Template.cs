namespace Kanelson.Contracts.Models;


[GenerateSerializer]
public record Template
{
    [Id(0)]
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Id(1)]
    public string Name { get; set; } = null!;
    
    [Id(2)]
    public ImmutableArray<TemplateQuestion> Questions { get; set; } = ImmutableArray<TemplateQuestion>.Empty;
}

[GenerateSerializer]
[Immutable]
public record TemplateSummary(Guid Id, string Name){ }



[GenerateSerializer]
public record TemplateQuestion : Question
{
    [Id(6)]
    public int Order { get; init; } = 0;
}