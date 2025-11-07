using Unary.Core;
using System;
using System.Runtime.CompilerServices;

namespace Unary.Recusant
{
    [Serializable]
    public readonly struct ScriptableObjectNetworkRef<T> : IEquatable<ScriptableObjectNetworkRef<T>>
    where T : BaseScriptableObject
    {
        public readonly int NetworkId;

        public readonly bool IsValid => NetworkId > 0;

        public ScriptableObjectNetworkRef(T obj)
        {
            NetworkId = (obj != null) ? obj.NetworkId : 0;
        }

        public readonly bool TryGetObject(out T obj)
        {
            return ScriptableObjectRegistry.Instance.LoadObject(NetworkId, out obj);
        }

        public readonly T GetObject()
        {
            if (TryGetObject(out var obj))
            {
                return obj;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (other is not ScriptableObjectNetworkRef<T>)
            {
                return false;
            }

            return Equals((ScriptableObjectNetworkRef<T>)other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode()
        {
            return NetworkId.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(ScriptableObjectNetworkRef<T> other)
        {
            return NetworkId.Equals(other.NetworkId);
        }

        public static bool operator ==(ScriptableObjectNetworkRef<T> a, ScriptableObjectNetworkRef<T> b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ScriptableObjectNetworkRef<T> a, ScriptableObjectNetworkRef<T> b)
        {
            return !a.Equals(b);
        }

        public override readonly string ToString()
        {
            return $"NetworkId: {NetworkId}";
        }
    }
}
