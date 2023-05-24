using Kanelson.Contracts.Models;

namespace Kanelson.Actors.Templates;

public record TemplateState
{
    public string OwnerId { get; set; } = null!;
    
    public Template Template { get; set; } = null!;
}