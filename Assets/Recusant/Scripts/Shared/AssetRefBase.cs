using Core;
using System;

namespace Recusant
{
    [Serializable]
    public abstract class AssetRefBase
    {
        public SerializableGuid AssetId;

#if UNITY_EDITOR

        public abstract object GetValueInternal();
        public abstract void SetValueInternal(object value);

#endif

        protected abstract void ResetValue();
    }
}
