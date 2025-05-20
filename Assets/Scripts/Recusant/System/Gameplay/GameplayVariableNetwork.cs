using Netick;
using System;
using UnityEngine;

namespace Recusant
{
    [Networked]
    [Serializable]
    public struct GameplayVariableNetwork : IEquatable<GameplayVariableNetwork>
    {
        [SerializeField]
        public NetworkBool Changed;
        [SerializeField]
        public NetworkString64 String;
        [SerializeField]
        public NetworkBool Bool;
        [SerializeField]
        public double Double;
        [SerializeField]
        public float Float1;
        [SerializeField]
        public float Float2;
        [SerializeField]
        public float Float3;

        public override readonly bool Equals(object obj)
        {
            return obj is GameplayVariableNetwork overrides && Equals(overrides);
        }

        public readonly bool Equals(GameplayVariableNetwork other)
        {
            return Changed == other.Changed &&
                String == other.String &&
                Bool == other.Bool &&
                Double == other.Double &&
                Float1 == other.Float1 &&
                Float2 == other.Float2 &&
                Float3 == other.Float3;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Changed, String, Bool, Double, Float1, Float2, Float3);
        }

        public static bool operator ==(GameplayVariableNetwork left, GameplayVariableNetwork right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GameplayVariableNetwork left, GameplayVariableNetwork right)
        {
            return !(left == right);
        }
    };

    public static class GameplayVariableExtensions
    {
        public static GameplayVariableNetwork ToNetwork(this AbstractVariable target)
        {
            GameplayVariableNetwork result = new();

            object targetObject = target.GetObject();

            switch (target.GetTypeEnum())
            {
                default:
                case GameplayType.None:
                    {
                        break;
                    }
                case GameplayType.String:
                    {
                        string converted = (string)targetObject;
                        converted = converted[..64];
                        result.String = converted;
                        break;
                    }
                case GameplayType.Bool:
                    {
                        bool converted = (bool)targetObject;
                        result.Bool = converted;
                        break;
                    }
                case GameplayType.Enum:
                    {
                        // Aquire stored enum type
                        Type enumType = target.GetTypeSystem();
                        // Convert it to the underlying type of the enum
                        // (you cant directly convert enums to doubles)
                        // enum Test : uint <- this is the underlying type
                        object converted = Convert.ChangeType(targetObject, enumType.GetEnumUnderlyingType());
                        // Only now can we conver to the double type
                        result.Double = (double)Convert.ChangeType(converted, typeof(double));
                        break;
                    }
                case GameplayType.Short:
                    {
                        short converted = (short)targetObject;
                        result.Double = (double)converted;
                        break;
                    }
                case GameplayType.UShort:
                    {
                        ushort converted = (ushort)targetObject;
                        result.Double = (double)converted;
                        break;
                    }
                case GameplayType.Int:
                    {
                        int converted = (int)targetObject;
                        result.Double = (double)converted;
                        break;
                    }
                case GameplayType.UInt:
                    {
                        uint converted = (uint)targetObject;
                        result.Double = (double)converted;
                        break;
                    }
                case GameplayType.Long:
                    {
                        long converted = (long)targetObject;
                        result.Double = (double)converted;
                        break;
                    }
                case GameplayType.ULong:
                    {
                        ulong converted = (ulong)targetObject;
                        result.Double = (double)converted;
                        break;
                    }
                case GameplayType.Double:
                    {
                        double doubleConverted = (double)targetObject;
                        result.Double = (double)doubleConverted;
                        break;
                    }
                case GameplayType.Float:
                    {
                        float converted = (float)targetObject;
                        result.Double = (double)converted;
                        break;
                    }
                case GameplayType.Vector2:
                    {
                        Vector2 converted = (Vector2)targetObject;
                        result.Double = (double)converted.x;
                        result.Float1 = (float)converted.y;
                        break;
                    }
                case GameplayType.Vector3:
                    {
                        Vector3 converted = (Vector3)targetObject;
                        result.Double = (double)converted.x;
                        result.Float1 = (float)converted.y;
                        result.Float2 = (float)converted.z;
                        break;
                    }
                case GameplayType.Vector4:
                    {
                        Vector4 converted = (Vector4)targetObject;
                        result.Double = (double)converted.x;
                        result.Float1 = (float)converted.y;
                        result.Float2 = (float)converted.z;
                        result.Float3 = (float)converted.w;
                        break;
                    }
                case GameplayType.Color:
                    {
                        Color converted = (Color)targetObject;
                        result.Double = (double)converted.r;
                        result.Float1 = (float)converted.g;
                        result.Float2 = (float)converted.b;
                        result.Float3 = (float)converted.a;
                        break;
                    }
            }
            return result;
        }

        public static void FromNetwork(this AbstractVariable target, GameplayVariableNetwork value)
        {
            switch (target.GetTypeEnum())
            {
                default:
                case GameplayType.None:
                    {
                        break;
                    }
                case GameplayType.String:
                    {
                        string converted = (string)value.String;
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.Bool:
                    {
                        bool converted = (bool)value.Bool;
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.Enum:
                    {
                        // Aquire stored enum type
                        Type enumType = target.GetTypeSystem();
                        // Convert it from double to the underlying enum type
                        // (you cant directly convert doubles to enums)
                        // enum Test : uint <- this is the underlying type
                        object converted = Convert.ChangeType(value.Double, enumType.GetEnumUnderlyingType());
                        // Now we can convert from the underlying type to the original enum type
                        // and assign this conversion to the variable
                        target.SetObject(Enum.ToObject(enumType, converted));
                        break;
                    }
                case GameplayType.Short:
                    {
                        short converted = (short)value.Double;
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.UShort:
                    {
                        ushort converted = (ushort)value.Double;
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.Int:
                    {
                        int converted = (int)value.Double;
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.UInt:
                    {
                        uint converted = (uint)value.Double;
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.Long:
                    {
                        long converted = (long)value.Double;
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.ULong:
                    {
                        ulong converted = (ulong)value.Double;
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.Double:
                    {
                        double converted = (double)value.Double;
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.Float:
                    {
                        float converted = (float)value.Double;
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.Vector2:
                    {
                        Vector2 converted = new()
                        {
                            x = (float)value.Double,
                            y = (float)value.Float1,
                        };
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.Vector3:
                    {
                        Vector3 converted = new()
                        {
                            x = (float)value.Double,
                            y = (float)value.Float1,
                            z = (float)value.Float2,
                        };
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.Vector4:
                    {
                        Vector4 converted = new()
                        {
                            x = (float)value.Double,
                            y = (float)value.Float1,
                            z = (float)value.Float2,
                            w = (float)value.Float3,
                        };
                        target.SetObject(converted);
                        break;
                    }
                case GameplayType.Color:
                    {
                        Color converted = new()
                        {
                            r = (float)value.Double,
                            g = (float)value.Float1,
                            b = (float)value.Float2,
                            a = (float)value.Float3,
                        };
                        target.SetObject(converted);
                        break;
                    }
            }
        }
    }
}
