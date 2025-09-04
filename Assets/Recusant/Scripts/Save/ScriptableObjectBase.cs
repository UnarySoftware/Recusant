using Netick;
using System;

namespace Recusant
{
    [Serializable]
    [Networked]
    public abstract class ScriptableObjectBase
    {
        public string Path = string.Empty;

#if UNITY_EDITOR

        public abstract object GetValueInternal();
        public abstract void SetValueInternal(object value);

#endif

        protected abstract void ResetValue();

        public ScriptableObjectBase(string value)
        {
            Path = value;

            if (ScriptableObjectRegistry.Instance == null)
            {
                return;
            }
        }
    }
}
