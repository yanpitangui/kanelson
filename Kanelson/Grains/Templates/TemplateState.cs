using Kanelson.Contracts.Models;

namespace Kanelson.Grains.Templates;

public record TemplateState
{
    public string OwnerId { get; set; } = null!;
    
    public Template Template { get; set; } = null!;
}