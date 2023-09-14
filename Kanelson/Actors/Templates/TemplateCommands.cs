namespace Kanelson.Actors.Templates;

public static class TemplateCommands
{
    public sealed record Upsert(Models.Template Template);

    internal sealed record Register(Guid Id);
}