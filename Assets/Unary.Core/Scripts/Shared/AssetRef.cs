using System;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Unary.Core
{
    [Serializable]
    public class AssetRef<T> : AssetRefBase
        where T : UnityEngine.Object
    {

#if UNITY_EDITOR

        public override object GetValueInternal()
        {
            return _value;
        }

        public override void SetValueInternal(object value)
        {
            _value = (T)value;
        }

        // Editor only constructor, since we need AssetDatabase access to resolve a Guid from an asset instance
        public AssetRef(T value)
        {
            AssetId = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(value)).ToSystem();
        }

#endif

        protected override void ResetValue()
        {
            _value = null;
        }

        [NonSerialized]
        private T _value = null;

        public AssetRef()
        {
            if (Bootstrap.Instance == null)
            {
                return;
            }
        }

        public AssetRef(Guid value)
        {
            if (Bootstrap.Instance == null)
            {
                return;
            }

            AssetId = value;
            _value = null;
        }

        public void Precache()
        {
            if (Bootstrap.Instance == null)
            {
                return;
            }

            T _ = Value;
        }

        protected virtual T LoadValue()
        {
            if (AssetPath != null)
            {
                return ContentLoader.Instance.LoadAsset<T>(AssetPath);
            }
            else
            {
                return ContentLoader.Instance.LoadAsset<T>(AssetId.Value);
            }
        }

        public virtual T Value
        {
            get
            {
                if (Bootstrap.Instance == null)
                {
                    return null;
                }

                if (_value != null)
                {
                    return _value;
                }

                _value = LoadValue();

                if (_value == null)
                {
                    Logger.Instance.Error("Failed to resolve an asset reference with GUID \"" + AssetId.Value.ToString() + "\"");
                    return _value;
                }

                return _value;
            }
        }
    }
}
