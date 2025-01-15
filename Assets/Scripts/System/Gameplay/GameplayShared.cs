using System;
using UnityEngine;

public enum GameplayGroup : int
{
    None = 0,
    Server = 1,
    Client = 2,
}

[Flags]
public enum GameplayFlag : uint
{
    None = 0,
    // This is a "Server" only flag, which means:
    // GameplayVariable - value gets replicated to clients
    // GameplayCommand - command call gets relayed to server for execution
    Replicated = 1 << 0
}

public enum GameplayType
{
    None,
    String,
    Bool,
    Enum,
    Short,
    UShort,
    Int,
    UInt,
    Long,
    ULong,
    Double,
    Float,
    Vector2,
    Vector3,
    Vector4,
    Color,
}

public class GameplayShared
{
    public static string StringifyType(Type type)
    {
        TypeCode typeCode = Type.GetTypeCode(type);

        string result = typeCode switch
        {
            TypeCode.String => "string",
            TypeCode.Boolean => "bool",
            TypeCode.Int16 => "short",
            TypeCode.UInt16 => "ushort",
            TypeCode.Int32 => "int",
            TypeCode.UInt32 => "uint",
            TypeCode.Int64 => "long",
            TypeCode.UInt64 => "ulong",
            TypeCode.Double => "double",
            TypeCode.Single => "float",
            _ when type == typeof(Vector2) => "vector2",
            _ when type == typeof(Vector3) => "vector3",
            _ when type == typeof(Vector4) => "vector4",
            _ when type == typeof(Color) => "color",
            _ => type.Name,
        };

        return result;
    }

    public static bool ValidateRangeForEnum(Type type, GameplayType valueType)
    {
        TypeCode typeCode = Type.GetTypeCode(type);

        bool isValid = valueType switch
        {
            GameplayType.None => false,
            GameplayType.Short => typeCode == TypeCode.Int16,
            GameplayType.UShort => typeCode == TypeCode.UInt16,
            GameplayType.Int => typeCode == TypeCode.Int32,
            GameplayType.UInt => typeCode == TypeCode.UInt32,
            GameplayType.Long => typeCode == TypeCode.Int64,
            GameplayType.ULong => typeCode == TypeCode.UInt64,
            GameplayType.Double => typeCode == TypeCode.Double,
            GameplayType.Float => typeCode == TypeCode.Single,
            GameplayType.Vector2 => typeCode == TypeCode.Single,
            GameplayType.Vector3 => typeCode == TypeCode.Single,
            GameplayType.Vector4 => typeCode == TypeCode.Single,
            GameplayType.Color => typeCode == TypeCode.Single,
            _ => false,
        };

        return isValid;
    }

    public static bool ValidateRangeForType(Type type, GameplayType valueType)
    {
        TypeCode typeCode = Type.GetTypeCode(type);

        bool isValid = typeCode switch
        {
            TypeCode.String => valueType == GameplayType.String,
            TypeCode.Boolean => valueType == GameplayType.Bool,
            TypeCode.Int16 => valueType == GameplayType.Short,
            TypeCode.UInt16 => valueType == GameplayType.UShort,
            TypeCode.Int32 => valueType == GameplayType.Int,
            TypeCode.UInt32 => valueType == GameplayType.UInt,
            TypeCode.Int64 => valueType == GameplayType.Long,
            TypeCode.UInt64 => valueType == GameplayType.ULong,
            TypeCode.Double => valueType == GameplayType.Double,
            TypeCode.Single => valueType == GameplayType.Float,
            _ when type == typeof(Vector2) => valueType == GameplayType.Float,
            _ when type == typeof(Vector3) => valueType == GameplayType.Float,
            _ when type == typeof(Vector4) => valueType == GameplayType.Float,
            _ when type == typeof(Color) => valueType == GameplayType.Float,
            _ => false,
        };

        return isValid;
    }

    public static GameplayType GetVariableType(Type type)
    {
        TypeCode typeCode = Type.GetTypeCode(type);

        GameplayType gameplayType = typeCode switch
        {
            TypeCode.String => GameplayType.String,
            TypeCode.Boolean => GameplayType.Bool,
            TypeCode.Int16 => GameplayType.Short,
            TypeCode.UInt16 => GameplayType.UShort,
            TypeCode.Int32 => GameplayType.Int,
            TypeCode.UInt32 => GameplayType.UInt,
            TypeCode.Int64 => GameplayType.Long,
            TypeCode.UInt64 => GameplayType.ULong,
            TypeCode.Double => GameplayType.Double,
            TypeCode.Single => GameplayType.Float,
            _ when type.IsEnum => GameplayType.Enum,
            _ when type == typeof(Vector2) => GameplayType.Vector2,
            _ when type == typeof(Vector3) => GameplayType.Vector3,
            _ when type == typeof(Vector4) => GameplayType.Vector4,
            _ when type == typeof(Color) => GameplayType.Color,
            _ => GameplayType.None,
        };

        return gameplayType;
    }
}
