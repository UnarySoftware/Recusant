using System;

namespace Unary.Core
{
    [Serializable]
    public abstract class AssetRefBase
    {
        public SerializableGuid AssetId;

        // Only used by SystemAssetInjectAttribute, every other form of asset referencing
        // has to be done with an AssetId above
        [NonSerialized]
        public string AssetPath = null;

        public bool CachingAllowed { get; set; } = true;
        public bool LoadingAllowed { get; set; } = true;

#if UNITY_EDITOR

        public abstract object GetValueInternal();
        public abstract void SetValueInternal(object value);

#endif

        protected abstract void ResetValue();
    }
}
