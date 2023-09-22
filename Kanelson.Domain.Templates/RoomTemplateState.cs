using Kanelson.Domain.Templates.Models;

namespace Kanelson.Domain.Templates;

public record RoomTemplateState
{
    public Template Template { get; set; } = null!;
}