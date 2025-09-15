using Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Recusant
{
    public abstract class AbstractVariable
    {
        public Action<int> OnChanged;

        protected int _id = -1;

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        protected string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        protected string _description;

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public abstract void SetObject(object value);
        public abstract object GetObject();
        public abstract object GetOriginalObject();
        public abstract GameplayGroup GetGroup();
        public abstract GameplayFlag GetFlags();
        public abstract GameplayType GetTypeEnum();
        public abstract Type GetTypeSystem();
        public abstract bool IsOriginal();
        public abstract void ResetToOriginal();
        public abstract bool IsRanged();
        public abstract double GetMinRange();
        public abstract double GetMaxRange();
    }

    // We are using generics for this class to be read from as fast as possible, even if you were to do so in Update()
    // Cost of using generics in this context means that we have to do type comparasement/boxing and unboxing in order to write
    // Consider this with caution if you are planning on updating gameplay variables frequently!
    [Serializable]
    public class GameplayVariable<T> : AbstractVariable where T : IComparable, IComparable<T>, IConvertible, IEquatable<T>
    {
        protected GameplayGroup _variableGroup = GameplayGroup.None;

        public override GameplayGroup GetGroup()
        {
            return _variableGroup;
        }

        protected GameplayFlag _variableFlags = GameplayFlag.None;

        public override GameplayFlag GetFlags()
        {
            return _variableFlags;
        }

        protected GameplayType _valueType = GameplayType.None;

        public override GameplayType GetTypeEnum()
        {
            return _valueType;
        }

        protected Type _valueSystemType = null;

        public override Type GetTypeSystem()
        {
            return _valueSystemType;
        }

        [SerializeField]
        protected T _currentValue;
        protected T _originalValue;

        public override bool IsOriginal()
        {
            return EqualityComparer<T>.Default.Equals(_currentValue, _originalValue);
        }

        public override void ResetToOriginal()
        {
            _currentValue = _originalValue;
            OnChanged?.Invoke(_id);
        }

        public T Get()
        {
            return _currentValue;
        }

        public virtual T ProcessValue(T value)
        {
            return value;
        }

        /*
        There REALLY shouldnt be any legitimate reason to change gameplay variables from code
        Gameplay variables are meant to be changed only from console or from configs
        public void Set(T value)
        {
            _currentValue = ProcessValue(value);
            OnChanged?.Invoke(_id);
        }
        */

        public override void SetObject(object value)
        {
            Type type = value.GetType();
            GameplayType variableType = GameplayShared.GetVariableType(type);

            if (variableType != _valueType)
            {
                Core.Logger.Instance.Error("GameplayVariable with type " + _valueType + " tried to be set with " + type.FullName);
                return;
            }

            T _newValue = ProcessValue((T)Convert.ChangeType(value, _valueSystemType));

            bool changed = !EqualityComparer<T>.Default.Equals(_currentValue, _newValue);

            _currentValue = _newValue;

            if (changed)
            {
                OnChanged?.Invoke(_id);
            }
        }

        public override object GetObject()
        {
            return _currentValue;
        }

        public override object GetOriginalObject()
        {
            return _originalValue;
        }

        public override bool IsRanged()
        {
            return false;
        }

        public override double GetMinRange()
        {
            return 0.0;
        }

        public override double GetMaxRange()
        {
            return 0.0;
        }

        public GameplayVariable(GameplayGroup group, GameplayFlag flags, T defaultValue, string description)
        {
            _valueSystemType = typeof(T);
            _valueType = GameplayShared.GetVariableType(_valueSystemType);

            if (_valueType == GameplayType.None)
            {
                Core.Logger.Instance.Error("GameplayVariable does not support type " + _valueSystemType.FullName);
                return;
            }

            _variableGroup = group;
            _variableFlags = flags;
            _description = description;

            _currentValue = defaultValue;
            _originalValue = defaultValue;

            if (Bootstrap.IsRuntime)
            {
                if (!GetType().IsSubclassOf(typeof(GameplayVariableRanged<,>)))
                {
                    OnChanged?.Invoke(_id);
                }
            }
        }
    }

    public class GameplayVariableRanged<T, U> : GameplayVariable<T>
        where T : IComparable, IComparable<T>, IConvertible, IEquatable<T>
        where U : IComparable, IComparable<T>, IConvertible, IEquatable<T>
    {
        protected bool _gotMinMaxes = false;
        protected double _valueMin = 0.0;
        protected double _valueMax = 0.0;

        public override bool IsRanged()
        {
            return true;
        }

        public override double GetMinRange()
        {
            if (!_gotMinMaxes)
            {
                return 0.0;
            }

            return _valueMin;
        }

        public override double GetMaxRange()
        {
            if (!_gotMinMaxes)
            {
                return 0.0;
            }

            return _valueMax;
        }

        public override T ProcessValue(T value)
        {
            return ClampWithRanges(value);
        }

        private T ClampWithRanges(T value)
        {
            if (!_gotMinMaxes)
            {
                return value;
            }

            switch (_valueType)
            {
                case GameplayType.Short:
                    {
                        short clamped = (short)Convert.ChangeType(value, typeof(short));
                        clamped = Math.Clamp(clamped, (short)_valueMin, (short)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                case GameplayType.UShort:
                    {
                        ushort clamped = (ushort)Convert.ChangeType(value, typeof(ushort));
                        clamped = Math.Clamp(clamped, (ushort)_valueMin, (ushort)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                case GameplayType.Int:
                    {
                        int clamped = (int)Convert.ChangeType(value, typeof(int));
                        clamped = Math.Clamp(clamped, (int)_valueMin, (int)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                case GameplayType.UInt:
                    {
                        uint clamped = (uint)Convert.ChangeType(value, typeof(uint));
                        clamped = Math.Clamp(clamped, (uint)_valueMin, (uint)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                case GameplayType.Long:
                    {
                        long clamped = (long)Convert.ChangeType(value, typeof(long));
                        clamped = Math.Clamp(clamped, (long)_valueMin, (long)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                case GameplayType.ULong:
                    {
                        ulong clamped = (ulong)Convert.ChangeType(value, typeof(ulong));
                        clamped = Math.Clamp(clamped, (ulong)_valueMin, (ulong)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                case GameplayType.Double:
                    {
                        double clamped = (double)Convert.ChangeType(value, typeof(double));
                        clamped = Math.Clamp(clamped, (double)_valueMin, (double)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                case GameplayType.Float:
                    {
                        float clamped = (float)Convert.ChangeType(value, typeof(float));
                        clamped = Math.Clamp(clamped, (float)_valueMin, (float)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                case GameplayType.Vector2:
                    {
                        Vector2 clamped = (Vector2)Convert.ChangeType(_currentValue, typeof(Vector2));
                        clamped.x = Math.Clamp(clamped.x, (float)_valueMin, (float)_valueMax);
                        clamped.y = Math.Clamp(clamped.y, (float)_valueMin, (float)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                case GameplayType.Vector3:
                    {
                        Vector3 clamped = (Vector3)Convert.ChangeType(_currentValue, typeof(Vector3));
                        clamped.x = Math.Clamp(clamped.x, (float)_valueMin, (float)_valueMax);
                        clamped.y = Math.Clamp(clamped.y, (float)_valueMin, (float)_valueMax);
                        clamped.z = Math.Clamp(clamped.z, (float)_valueMin, (float)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                case GameplayType.Vector4:
                    {
                        Vector4 clamped = (Vector4)Convert.ChangeType(_currentValue, typeof(Vector4));
                        clamped.x = Math.Clamp(clamped.x, (float)_valueMin, (float)_valueMax);
                        clamped.y = Math.Clamp(clamped.y, (float)_valueMin, (float)_valueMax);
                        clamped.z = Math.Clamp(clamped.z, (float)_valueMin, (float)_valueMax);
                        clamped.w = Math.Clamp(clamped.w, (float)_valueMin, (float)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                case GameplayType.Color:
                    {
                        Color clamped = (Color)Convert.ChangeType(_currentValue, typeof(Color));
                        clamped.r = Math.Clamp(clamped.r, (float)_valueMin, (float)_valueMax);
                        clamped.g = Math.Clamp(clamped.g, (float)_valueMin, (float)_valueMax);
                        clamped.b = Math.Clamp(clamped.b, (float)_valueMin, (float)_valueMax);
                        clamped.a = Math.Clamp(clamped.a, (float)_valueMin, (float)_valueMax);
                        return (T)Convert.ChangeType(clamped, _valueSystemType);
                    }
                default:
                    {
                        return value;
                    }
            }
        }

        public GameplayVariableRanged(GameplayGroup group, GameplayFlag flags, T defaultValue, U rangeMin, U rangeMax, string description) :
            base(group, flags, defaultValue, description)
        {
            if (!GameplayShared.ValidateRangeForEnum(typeof(T), _valueType))
            {
                Core.Logger.Instance.Error("GameplayVariable type " + _valueSystemType.FullName + " does not support type " + typeof(U).FullName + " as a range");
                return;
            }

            _valueMin = (double)Convert.ChangeType(rangeMin, typeof(double));
            _valueMax = (double)Convert.ChangeType(rangeMax, typeof(double));
            _gotMinMaxes = true;

            _originalValue = ClampWithRanges(defaultValue);
            _currentValue = ClampWithRanges(defaultValue);

            if (Bootstrap.IsRuntime)
            {
                OnChanged?.Invoke(_id);
            }
        }
    }
}
