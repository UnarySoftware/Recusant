using System;
using UnityEngine;

namespace Recusant
{
    public abstract class BaseScriptableObject : ScriptableObject
    {
        [NonSerialized]
        public int NetworkId = 0;

        private static bool _precached = false;

        public abstract void Precache();

        public void PrecacheInternal()
        {
            if (_precached)
            {
                return;
            }

            Precache();
            _precached = true;
        }
    }
}
