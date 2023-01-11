namespace Kanelson.Shared;

public record AnswerColor(string Code)
{
        
    private static readonly AnswerColor[] _colors = new AnswerColor [] { "#ffa602",
        "#eb670f", "#eb21b3c", "#26890c", "#0aa3a3", "#1368ce", "#46178f" };
    
    
    public static implicit operator AnswerColor(string code) => new AnswerColor(code);
    
    public static AnswerColor GetColor(int idx)
    {
        return idx < _colors.Length ? new AnswerColor(_colors[idx]) : new AnswerColor(_colors[Random.Shared.Next(0, _colors.Length)]);
    }
}