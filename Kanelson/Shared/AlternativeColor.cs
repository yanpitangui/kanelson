namespace Kanelson.Shared;

public record AlternativeColor(string Code)
{
    private static readonly AlternativeColor[] _colors =
    [
        "alt-tile-a",
        "alt-tile-b",
        "alt-tile-c",
        "alt-tile-d"
    ];

    public static implicit operator AlternativeColor(string code) => new(code);

    public static AlternativeColor GetColor(int idx) =>
        new AlternativeColor(_colors[idx % _colors.Length].Code);
}