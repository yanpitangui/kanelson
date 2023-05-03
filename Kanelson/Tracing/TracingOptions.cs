namespace Kanelson.Tracing;

public record TracingOptions
{
    public bool Enabled { get; init; }
    
    public required string Uri { get; init; }
}