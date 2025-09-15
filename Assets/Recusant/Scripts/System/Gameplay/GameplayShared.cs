using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Recusant
{
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
        public static object ClampWithRanges(object value, GameplayType type, double Min, double Max)
        {
            switch (type)
            {
                case GameplayType.Short:
                    {
                        short clamped = (short)Convert.ChangeType(value, typeof(short));
                        clamped = Math.Clamp(clamped, (short)Min, (short)Max);
                        return clamped;
                    }
                case GameplayType.UShort:
                    {
                        ushort clamped = (ushort)Convert.ChangeType(value, typeof(ushort));
                        clamped = Math.Clamp(clamped, (ushort)Min, (ushort)Max);
                        return clamped;
                    }
                case GameplayType.Int:
                    {
                        int clamped = (int)Convert.ChangeType(value, typeof(int));
                        clamped = Math.Clamp(clamped, (int)Min, (int)Max);
                        return clamped;
                    }
                case GameplayType.UInt:
                    {
                        uint clamped = (uint)Convert.ChangeType(value, typeof(uint));
                        clamped = Math.Clamp(clamped, (uint)Min, (uint)Max);
                        return clamped;
                    }
                case GameplayType.Long:
                    {
                        long clamped = (long)Convert.ChangeType(value, typeof(long));
                        clamped = Math.Clamp(clamped, (long)Min, (long)Max);
                        return clamped;
                    }
                case GameplayType.ULong:
                    {
                        ulong clamped = (ulong)Convert.ChangeType(value, typeof(ulong));
                        clamped = Math.Clamp(clamped, (ulong)Min, (ulong)Max);
                        return clamped;
                    }
                case GameplayType.Double:
                    {
                        double clamped = (double)Convert.ChangeType(value, typeof(double));
                        clamped = Math.Clamp(clamped, (double)Min, (double)Max);
                        return clamped;
                    }
                case GameplayType.Float:
                    {
                        float clamped = (float)Convert.ChangeType(value, typeof(float));
                        clamped = Math.Clamp(clamped, (float)Min, (float)Max);
                        return clamped;
                    }
                case GameplayType.Vector2:
                    {
                        Vector2 clamped = (Vector2)Convert.ChangeType(value, typeof(Vector2));
                        clamped.x = Math.Clamp(clamped.x, (float)Min, (float)Max);
                        clamped.y = Math.Clamp(clamped.y, (float)Min, (float)Max);
                        return clamped;
                    }
                case GameplayType.Vector3:
                    {
                        Vector3 clamped = (Vector3)Convert.ChangeType(value, typeof(Vector3));
                        clamped.x = Math.Clamp(clamped.x, (float)Min, (float)Max);
                        clamped.y = Math.Clamp(clamped.y, (float)Min, (float)Max);
                        clamped.z = Math.Clamp(clamped.z, (float)Min, (float)Max);
                        return clamped;
                    }
                case GameplayType.Vector4:
                    {
                        Vector4 clamped = (Vector4)Convert.ChangeType(value, typeof(Vector4));
                        clamped.x = Math.Clamp(clamped.x, (float)Min, (float)Max);
                        clamped.y = Math.Clamp(clamped.y, (float)Min, (float)Max);
                        clamped.z = Math.Clamp(clamped.z, (float)Min, (float)Max);
                        clamped.w = Math.Clamp(clamped.w, (float)Min, (float)Max);
                        return clamped;
                    }
                case GameplayType.Color:
                    {
                        Color clamped = (Color)Convert.ChangeType(value, typeof(Color));
                        clamped.r = Math.Clamp(clamped.r, (float)Min, (float)Max);
                        clamped.g = Math.Clamp(clamped.g, (float)Min, (float)Max);
                        clamped.b = Math.Clamp(clamped.b, (float)Min, (float)Max);
                        clamped.a = Math.Clamp(clamped.a, (float)Min, (float)Max);
                        return clamped;
                    }
                default:
                    {
                        return value;
                    }
            }
        }

        public struct GameplayCommandArgument
        {
            public GameplayType gameplayType;
            public Type systemType;
        }

        public static List<GameplayCommandArgument> GetArgumentTypes(MethodInfo info)
        {
            List<GameplayCommandArgument> result = new();

            ParameterInfo[] parameters = info.GetParameters();

            foreach (var param in parameters)
            {
                result.Add(new GameplayCommandArgument()
                {
                    gameplayType = GetVariableType(param.ParameterType),
                    systemType = param.ParameterType
                });
            }

            return result;
        }

        // defaultValue - value from clamped range
        public static object GetValueFromNode(GameplayType gameplayType, Type systemType, object node, object defaultValue = null)
        {
            switch (gameplayType)
            {
                default:
                case GameplayType.None:
                    {
                        return null;
                    }
                case GameplayType.String:
                    {
                        if (node is string)
                        {
                            return node;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                case GameplayType.Bool:
                    {
                        if (node is double v)
                        {
                            return (v != 0.0);
                        }
                        else
                        {
                            return false;
                        }
                    }
                case GameplayType.Enum:
                    {
                        if (node is string)
                        {
                            return Enum.Parse(systemType, (string)node);
                        }
                        else
                        {
                            return Convert.ChangeType(0, systemType);
                        }
                    }
                case GameplayType.Short:
                    {
                        if (node is double)
                        {
                            return (short)Convert.ChangeType((double)node, typeof(short));
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                return (short)Convert.ChangeType(defaultValue, typeof(short));
                            }
                            return (short)0;
                        }
                    }
                case GameplayType.UShort:
                    {
                        if (node is double)
                        {
                            return (short)Convert.ChangeType((double)node, typeof(ushort));
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                return (ushort)Convert.ChangeType(defaultValue, typeof(ushort));
                            }
                            return (ushort)0;
                        }
                    }
                case GameplayType.Int:
                    {
                        if (node is double)
                        {
                            return (short)Convert.ChangeType((double)node, typeof(int));
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                return (int)Convert.ChangeType(defaultValue, typeof(int));
                            }
                            return (int)0;
                        }
                    }
                case GameplayType.UInt:
                    {
                        if (node is double)
                        {
                            return (short)Convert.ChangeType((double)node, typeof(uint));
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                return (uint)Convert.ChangeType(defaultValue, typeof(uint));
                            }
                            return (uint)0;
                        }
                    }
                case GameplayType.Long:
                    {
                        if (node is double)
                        {
                            return (short)Convert.ChangeType((double)node, typeof(long));
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                return (long)Convert.ChangeType(defaultValue, typeof(long));
                            }
                            return (long)0;
                        }
                    }
                case GameplayType.ULong:
                    {
                        if (node is double)
                        {
                            return (short)Convert.ChangeType((double)node, typeof(ulong));
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                return (ulong)Convert.ChangeType(defaultValue, typeof(ulong));
                            }
                            return (ulong)0;
                        }
                    }
                case GameplayType.Double:
                    {
                        if (node is double)
                        {
                            return (double)node;
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                return (double)Convert.ChangeType(defaultValue, typeof(double));
                            }
                            return 0.0;
                        }
                    }
                case GameplayType.Float:
                    {
                        if (node is double)
                        {
                            return (float)Convert.ChangeType((double)node, typeof(float));
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                return (float)Convert.ChangeType(defaultValue, typeof(float));
                            }
                            return 0.0f;
                        }
                    }
                case GameplayType.Vector2:
                    {
                        if (node is List<object>)
                        {
                            List<object> array = (List<object>)node;

                            if (array.Count == 2 && array[0] is double && array[1] is double)
                            {
                                return new Vector2((float)Convert.ChangeType(array[0], typeof(float)),
                                    (float)Convert.ChangeType(array[1], typeof(float)));
                            }
                            else
                            {
                                if (defaultValue != null)
                                {
                                    float defaultCasted = (float)Convert.ChangeType(defaultValue, typeof(float));
                                    return new Vector2(defaultCasted, defaultCasted);
                                }
                                return Vector2.zero;
                            }
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                float defaultCasted = (float)Convert.ChangeType(defaultValue, typeof(float));
                                return new Vector2(defaultCasted, defaultCasted);
                            }
                            return Vector2.zero;
                        }
                    }
                case GameplayType.Vector3:
                    {
                        if (node is List<object>)
                        {
                            List<object> array = (List<object>)node;

                            if (array.Count == 3 && array[0] is double && array[1] is double && array[2] is double)
                            {
                                return new Vector3((float)Convert.ChangeType(array[0], typeof(float)),
                                    (float)Convert.ChangeType(array[1], typeof(float)),
                                    (float)Convert.ChangeType(array[2], typeof(float)));
                            }
                            else
                            {
                                if (defaultValue != null)
                                {
                                    float defaultCasted = (float)Convert.ChangeType(defaultValue, typeof(float));
                                    return new Vector3(defaultCasted, defaultCasted, defaultCasted);
                                }
                                return Vector3.zero;
                            }
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                float defaultCasted = (float)Convert.ChangeType(defaultValue, typeof(float));
                                return new Vector3(defaultCasted, defaultCasted, defaultCasted);
                            }
                            return Vector3.zero;
                        }
                    }
                case GameplayType.Vector4:
                    {
                        if (node is List<object>)
                        {
                            List<object> array = (List<object>)node;

                            if (array.Count == 4 && array[0] is double && array[1] is double && array[2] is double && array[3] is double)
                            {
                                return new Vector4((float)Convert.ChangeType(array[0], typeof(float)),
                                    (float)Convert.ChangeType(array[1], typeof(float)),
                                    (float)Convert.ChangeType(array[2], typeof(float)),
                                    (float)Convert.ChangeType(array[3], typeof(float)));
                            }
                            else
                            {
                                if (defaultValue != null)
                                {
                                    float defaultCasted = (float)Convert.ChangeType(defaultValue, typeof(float));
                                    return new Vector4(defaultCasted, defaultCasted, defaultCasted, defaultCasted);
                                }
                                return Vector4.zero;
                            }
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                float defaultCasted = (float)Convert.ChangeType(defaultValue, typeof(float));
                                return new Vector4(defaultCasted, defaultCasted, defaultCasted, defaultCasted);
                            }
                            return Vector4.zero;
                        }
                    }
                case GameplayType.Color:
                    {
                        if (node is List<object>)
                        {
                            List<object> array = (List<object>)node;

                            if (array.Count == 4 && array[0] is double && array[1] is double && array[2] is double && array[3] is double)
                            {
                                return new Color((float)Convert.ChangeType(array[0], typeof(float)),
                                    (float)Convert.ChangeType(array[1], typeof(float)),
                                    (float)Convert.ChangeType(array[2], typeof(float)),
                                    (float)Convert.ChangeType(array[3], typeof(float)));
                            }
                            else
                            {
                                if (defaultValue != null)
                                {
                                    float defaultCasted = (float)Convert.ChangeType(defaultValue, typeof(float));
                                    return new Color(defaultCasted, defaultCasted, defaultCasted, defaultCasted);
                                }
                                return Color.white;
                            }
                        }
                        else
                        {
                            if (defaultValue != null)
                            {
                                float defaultCasted = (float)Convert.ChangeType(defaultValue, typeof(float));
                                return new Color(defaultCasted, defaultCasted, defaultCasted, defaultCasted);
                            }
                            return Color.white;
                        }
                    }
            }
        }

        public static string StringifyVariable(object value, GameplayType gameplayType)
        {
            switch (gameplayType)
            {
                default:
                case GameplayType.None:
                    {
                        return "None";
                    }
                case GameplayType.String:
                    {
                        string result = (string)value;
                        return "\"" + result + "\"";
                    }
                case GameplayType.Enum:
                    {
                        return value.ToString();
                    }
                case GameplayType.Bool:
                    {
                        return (bool)value ? "1" : "0";
                    }
                case GameplayType.Short:
                    {
                        short result = (short)Convert.ChangeType(value, typeof(short));
                        return result.ToString();
                    }
                case GameplayType.UShort:
                    {
                        ushort result = (ushort)Convert.ChangeType(value, typeof(ushort));
                        return result.ToString();
                    }
                case GameplayType.Int:
                    {
                        int result = (int)Convert.ChangeType(value, typeof(int));
                        return result.ToString();
                    }
                case GameplayType.UInt:
                    {
                        uint result = (uint)Convert.ChangeType(value, typeof(uint));
                        return result.ToString();
                    }
                case GameplayType.Long:
                    {
                        long result = (long)Convert.ChangeType(value, typeof(long));
                        return result.ToString();
                    }
                case GameplayType.ULong:
                    {
                        ulong result = (ulong)Convert.ChangeType(value, typeof(ulong));
                        return result.ToString();
                    }
                case GameplayType.Double:
                    {
                        double result = (double)Convert.ChangeType(value, typeof(double));
                        return string.Format(CultureInfo.InvariantCulture, "{0:0.00}", result);
                    }
                case GameplayType.Float:
                    {
                        float result = (float)Convert.ChangeType(value, typeof(float));
                        return string.Format(CultureInfo.InvariantCulture, "{0:0.00}", result);
                    }
                case GameplayType.Vector2:
                    {
                        Vector2 result = (Vector2)value;
                        return string.Format(CultureInfo.InvariantCulture, "[{0:0.00}, {1:0.00}]", result.x, result.y);
                    }
                case GameplayType.Vector3:
                    {
                        Vector3 result = (Vector3)value;
                        return string.Format(CultureInfo.InvariantCulture, "[{0:0.00}, {1:0.00}, {2:0.00}]", result.x, result.y, result.z);
                    }
                case GameplayType.Vector4:
                    {
                        Vector4 result = (Vector4)value;
                        return string.Format(CultureInfo.InvariantCulture, "[{0:0.00}, {1:0.00}, {2:0.00}, {3:0.00}]", result.x, result.y, result.z, result.w);
                    }
                case GameplayType.Color:
                    {
                        Color result = (Color)value;
                        return string.Format(CultureInfo.InvariantCulture, "[{0:0.00}, {1:0.00}, {2:0.00}, {3:0.00}]", result.r, result.g, result.b, result.a);
                    }
            }
        }

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

        public static string StringifyRanges(double min, double max, GameplayType type)
        {
            StringBuilder builder = new();

            builder.Append('[');

            object minObject = min;

            builder.Append(StringifyVariable(minObject, type));

            builder.Append(", ");

            object maxObject = max;

            builder.Append(StringifyVariable(maxObject, type));

            builder.Append("] ");

            return builder.ToString();
        }

        public static GameplayType GetRangeForType(Type type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);

            GameplayType result = typeCode switch
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
                _ when type == typeof(Vector2) => GameplayType.Float,
                _ when type == typeof(Vector3) => GameplayType.Float,
                _ when type == typeof(Vector4) => GameplayType.Float,
                _ when type == typeof(Color) => GameplayType.Float,
                _ => GameplayType.None,
            };

            return result;
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
}
