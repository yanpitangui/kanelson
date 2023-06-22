namespace Kanelson.Shared;

public record AlternativeColor(string Code)
{
        
    private static readonly AlternativeColor[] _colors = { "#ffa602",
        "#eb670f", "#eb2b3c", "#26890c", "#0aa3a3", "#1368ce", "#46178f" };
    
    
    public static implicit operator AlternativeColor(string code) => new(code);
    
    public static AlternativeColor GetColor(int idx)
    {
        return idx < _colors.Length ? new AlternativeColor(_colors[idx]) : new AlternativeColor(_colors[Random.Shared.Next(0, _colors.Length)]);
    }
}