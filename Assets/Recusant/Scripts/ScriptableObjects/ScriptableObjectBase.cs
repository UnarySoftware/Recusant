using Core;
using System;

namespace Recusant
{
    [Serializable]
    public abstract class ScriptableObjectBase
    {
        public SerializableGuid UniqueId;

#if UNITY_EDITOR

        public abstract object GetValueInternal();
        public abstract void SetValueInternal(object value);

#endif

        protected abstract void ResetValue();

        public ScriptableObjectBase(Guid value)
        {
            UniqueId = value;
        }
    }
}
