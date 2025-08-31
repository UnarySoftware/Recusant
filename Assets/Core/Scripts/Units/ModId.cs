using System;

namespace Core
{
    public readonly struct ModId : IEquatable<ModId>
    {
        public readonly string Value;

        public ModId(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public bool Equals(ModId other)
        {
            return other.Value == Value;
        }

        public override bool Equals(object obj)
        {
            return Equals((ModId)obj);
        }
    }
}
