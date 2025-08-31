using System;
using UnityEngine;

namespace Recusant
{
    public abstract class BaseScriptableObject : ScriptableObject
    {
        public Core.SerializableGuid UniqueId;

        [NonSerialized]
        public int IndexId = -1;

        public abstract void Precache();
    }
}
