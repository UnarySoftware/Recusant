using Core;
using System;
using UnityEngine;

namespace Recusant
{
    [Serializable]
    public class PrefabRef<T> : AssetRef<GameObject>
        where T : MonoBehaviour
    {
        public PrefabRef(Guid value) : base(value)
        {

        }

        /*
        public T Component
        {
            get
            {
                GameObject gameObject = Value;
                return gameObject.GetComponent<T>();
            }
        }
        */
    }
}
