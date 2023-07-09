using Kanelson.Models;

namespace Kanelson.Actors.Templates;

public record TemplateState
{
    public string OwnerId { get; set; } = null!;
    
    public Models.Template Template { get; set; } = null!;
}