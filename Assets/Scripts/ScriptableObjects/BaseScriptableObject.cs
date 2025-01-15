using UnityEngine;

public abstract class BaseScriptableObject : ScriptableObject
{
    [HideInInspector]
    public uint Id = uint.MaxValue;
}
