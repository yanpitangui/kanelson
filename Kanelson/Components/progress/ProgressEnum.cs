namespace Kanelson.Components.progress;


    public sealed class ProgressSize : EnumValue
    {
        public static readonly ProgressSize Small = new(nameof(Small), 1);
        public static readonly ProgressSize Default = new(nameof(Default), 2);

        private ProgressSize(string name, int value) : base(name.ToLowerInvariant(), value)
        {
        }
    }

    public sealed class ProgressType : EnumValue
    {
        public static readonly ProgressType Line = new(nameof(Line), 1);
        public static readonly ProgressType Circle = new(nameof(Circle), 2);
        public static readonly ProgressType Dashboard = new(nameof(Dashboard), 3);

        private ProgressType(string name, int value) : base(name.ToLowerInvariant(), value)
        {
        }
    }

    public sealed class ProgressStatus : EnumValue
    {
        public static readonly ProgressStatus Success = new(nameof(Success), 1);
        public static readonly ProgressStatus Exception = new(nameof(Exception), 2);
        public static readonly ProgressStatus Normal = new(nameof(Normal), 3);
        public static readonly ProgressStatus Active = new(nameof(Active), 4);

        private ProgressStatus(string name, int value) : base(name.ToLowerInvariant(), value)
        {
        }
    }

    public sealed class ProgressStrokeLinecap : EnumValue
    {
        public static readonly ProgressStrokeLinecap Round = new(nameof(Round), 1);
        public static readonly ProgressStrokeLinecap Square = new(nameof(Square), 2);

        private ProgressStrokeLinecap(string name, int value) : base(name.ToLowerInvariant(), value)
        {
        }
    }

    public sealed class ProgressGapPosition : EnumValue
    {
        public static readonly ProgressGapPosition Top = new(nameof(Top), 1);
        public static readonly ProgressGapPosition Bottom = new(nameof(Bottom), 2);
        public static readonly ProgressGapPosition Left = new(nameof(Left), 3);
        public static readonly ProgressGapPosition Right = new(nameof(Right), 4);

        private ProgressGapPosition(string name, int value) : base(name.ToLowerInvariant(), value)
        {
        }
    }


public abstract class EnumValue
{
    
    public string Name { get; init; }
    
    public int Value { get; init; }
    protected EnumValue(string name, int value)
    {
        Value = value;
        Name = name;
    }
}