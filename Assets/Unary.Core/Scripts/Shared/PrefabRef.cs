using System;
using UnityEngine;

namespace Unary.Core
{
    [Serializable]
    public class PrefabRef<T> : AssetRef<GameObject>
        where T : MonoBehaviour
    {

    }
}
