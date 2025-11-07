using System;
using UnityEngine;

namespace Unary.Core
{
    [Serializable]
    public class SerializableGuid : IEquatable<SerializableGuid>
    {
        public override bool Equals(object obj)
        {
            return obj is SerializableGuid other && Equals(other);
        }

        public bool Equals(SerializableGuid other)
        {
            if (_bytes == null && other._bytes == null)
            {
                return true;
            }

            if (_bytes == null || other._bytes == null)
            {
                return false;
            }

            if (_bytes.Length != other._bytes.Length)
            {
                return false;
            }

            for (int i = 0; i < _bytes.Length; i++)
            {
                if (_bytes[i] != other._bytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (_bytes != null)
            {
                foreach (var targetByte in _bytes)
                {
                    hash = HashCode.Combine(hash, targetByte);
                }
            }
            return hash;
        }


        private Guid _id = Guid.Empty;

        [HideInInspector]
        [SerializeField]
        private byte[] _bytes;

        public static implicit operator SerializableGuid(Guid value)
        {
            SerializableGuid result = new()
            {
                Value = value
            };
            return result;
        }

        public static implicit operator Guid(SerializableGuid value)
        {
            return value.Value;
        }

        public bool IsDefault()
        {
            return Value == Guid.Empty;
        }

        public Guid Value
        {
            get
            {
                if (_id == Guid.Empty && _bytes.Length == 16)
                {
                    _id = new Guid(_bytes);
                }

                return _id;
            }
            set
            {
                _id = value;
                _bytes = _id.ToByteArray();
            }
        }

        public void OnAfterDeserialize()
        {
            if (_id == Guid.Empty && _bytes.Length == 16)
            {
                _id = new Guid(_bytes);
            }
        }
    }
}
