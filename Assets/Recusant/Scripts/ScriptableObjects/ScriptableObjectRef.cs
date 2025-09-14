using Core;
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

        public ScriptableObjectRef(Guid value) : base(value)
        {
        }

        public T Value
        {
            get
            {
                if (_value != null)
                {
                    return _value;
                }

                if (!ScriptableObjectRegistry.Instance.LoadObject(UniqueId, out _value))
                {
                    Logger.Instance.Error("Failed to resolve ScriptableObject reference with GUID \"" + UniqueId.Value.ToString() + "\"");
                }

                return _value;
            }
        }
    }
}
