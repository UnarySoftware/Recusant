using Core;
using System;

namespace Recusant
{
    [Serializable]
    public class ScriptableObjectRef<T> : AssetRef<T>
        where T : BaseScriptableObject
    {
        public ScriptableObjectRef(Guid value) : base(value)
        {

        }

        protected override T LoadValue()
        {
            if (ScriptableObjectRegistry.Instance.LoadObject(AssetId.Value, out T result))
            {
                return result;
            }
            Logger.Instance.Error("Failed to resolve ScriptableObject reference with GUID \"" + AssetId.Value.ToString() + "\"");
            return null;
        }
    }
}
