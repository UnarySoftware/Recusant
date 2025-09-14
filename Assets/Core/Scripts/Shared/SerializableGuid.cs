using System;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class SerializableGuid
    {
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
                if (_id == Guid.Empty)
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
            if (_id == Guid.Empty)
            {
                _id = new Guid(_bytes);
            }
        }
    }
}
