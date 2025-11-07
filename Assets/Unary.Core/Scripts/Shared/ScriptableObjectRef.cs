using System;

namespace Unary.Core
{
    [Serializable]
    public class ScriptableObjectRef<T> : AssetRef<T>
        where T : BaseScriptableObject
    {
        protected override T LoadValue()
        {
            if (!ScriptableObjectRegistry.Instance.LoadObject(AssetId.Value, out T result))
            {
                Logger.Instance.Error("Failed to resolve ScriptableObject reference with GUID \"" + AssetId.Value.ToString() + "\"");
                return null;
            }

            return result;
        }
    }
}
