using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class GameplayCommandRange : Attribute
{
    public GameplayType Type { get; protected set; } = GameplayType.None;
    public double DefaultValue { get; protected set; } = 0.0;
    public double Min { get; protected set; } = 0.0;
    public double Max { get; protected set; } = 0.0;
}

public class GameplayCommandShort : GameplayCommandRange
{
    public GameplayCommandShort(short defaultValue = 0, short min = short.MinValue, short max = short.MaxValue)
    {
        Type = GameplayType.Short;
        DefaultValue = (double)Math.Clamp(defaultValue, min, max);
        Min = (double)min;
        Max = (double)max;
    }
}

public class GameplayCommandUShort : GameplayCommandRange
{
    public GameplayCommandUShort(ushort defaultValue = 0, ushort min = ushort.MinValue, ushort max = ushort.MaxValue)
    {
        Type = GameplayType.UShort;
        DefaultValue = (double)Math.Clamp(defaultValue, min, max);
        Min = (double)min;
        Max = (double)max;
    }
}

public class GameplayCommandInt : GameplayCommandRange
{
    public GameplayCommandInt(int defaultValue = 0, int min = int.MinValue, int max = int.MaxValue)
    {
        Type = GameplayType.Int;
        DefaultValue = (double)Math.Clamp(defaultValue, min, max);
        Min = (double)min;
        Max = (double)max;
    }
}

public class GameplayCommandUInt : GameplayCommandRange
{
    public GameplayCommandUInt(uint defaultValue = 0, uint min = uint.MinValue, uint max = uint.MaxValue)
    {
        Type = GameplayType.UInt;
        DefaultValue = (double)Math.Clamp(defaultValue, min, max);
        Min = (double)min;
        Max = (double)max;
    }
}

public class GameplayCommandLong : GameplayCommandRange
{
    public GameplayCommandLong(long defaultValue = 0, long min = long.MinValue, long max = long.MaxValue)
    {
        Type = GameplayType.Long;
        DefaultValue = (double)Math.Clamp(defaultValue, min, max);
        Min = (double)min;
        Max = (double)max;
    }
}

public class GameplayCommandULong : GameplayCommandRange
{
    public GameplayCommandULong(ulong defaultValue = 0, ulong min = ulong.MinValue, ulong max = ulong.MaxValue)
    {
        Type = GameplayType.ULong;
        DefaultValue = (double)Math.Clamp(defaultValue, min, max);
        Min = (double)min;
        Max = (double)max;
    }
}

public class GameplayCommandDouble : GameplayCommandRange
{
    public GameplayCommandDouble(double defaultValue = 0.0, double min = double.MinValue, double max = double.MaxValue)
    {
        Type = GameplayType.Double;
        DefaultValue = (double)Math.Clamp(defaultValue, min, max);
        Min = min;
        Max = max;
    }
}

public class GameplayCommandFloat : GameplayCommandRange
{
    public GameplayCommandFloat(float defaultValue = 0.0f, float min = float.MinValue, float max = float.MaxValue)
    {
        Type = GameplayType.Float;
        DefaultValue = (double)Math.Clamp(defaultValue, min, max);
        Min = (double)min;
        Max = (double)max;
    }
}

public class GameplayCommandIgnore : GameplayCommandRange
{
    public GameplayCommandIgnore()
    {
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class GameplayCommandAttribute : Attribute
{
    public GameplayGroup Group { get; private set; } = GameplayGroup.None;
    public GameplayFlag Flags { get; private set; } = GameplayFlag.None;
    public string Description { get; private set; } = string.Empty;

    public GameplayCommandAttribute(GameplayGroup group, GameplayFlag flags, string description)
    {
        Group = group;
        Flags = flags;
        Description = description;
    }
}
