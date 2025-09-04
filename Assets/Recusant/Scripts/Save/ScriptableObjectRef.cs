using System;

namespace Recusant
{
    [Serializable]
    public class ScriptableObjectRef<T> : ScriptableObjectBase
        where T : BaseScriptableObject
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

#endif

        protected override void ResetValue()
        {
            _value = null;
        }

        [NonSerialized]
        private T _value = null;

        public ScriptableObjectRef(string value) : base(value)
        {
        }

        public T Get()
        {
            if (_value != null)
            {
                return _value;
            }

            if (Path != string.Empty)
            {
                if(!ScriptableObjectRegistry.Instance.LoadObject<T>(Path, out _value))
                {
                    Core.Logger.Instance.Error("Failed to resolve ScriptableObject reference with path \"" + Path + "\"");
                }
            }
            else
            {
                Core.Logger.Instance.Error("Failed to resolve ScriptableObject reference");
            }

            return _value;
        }

        public void Set(T value, string path)
        {
            _value = value;

            if (path != string.Empty)
            {
                Path = path;
            }
        }
    }
}
