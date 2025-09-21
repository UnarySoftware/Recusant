using System;
using UnityEngine;

namespace Recusant
{
    public abstract class BaseScriptableObject : ScriptableObject
    {
        [NonSerialized]
        public int NetworkId = 0;

        public virtual void Precache()
        {

        }
    }
}
