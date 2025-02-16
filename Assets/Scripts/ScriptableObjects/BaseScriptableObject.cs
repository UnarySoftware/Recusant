using System;
using UnityEngine;

public abstract class BaseScriptableObject : ScriptableObject
{
    public SerializableGuid UniqueId;

    [NonSerialized]
    public int IndexId = -1;

    public abstract void Precache();
}
