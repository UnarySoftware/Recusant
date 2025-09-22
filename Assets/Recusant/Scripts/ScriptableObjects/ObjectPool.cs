using System.Collections.Generic;
using UnityEngine;

namespace Recusant
{
    [CreateAssetMenu(fileName = "ObjectPool", menuName = "Recusant/Data/ObjectPool")]
    public class ObjectPool : BaseScriptableObject
    {
        public AssetRef<GameObject> Prefab;

        [Range(1, 256)]
        public int Count = 1;

        public List<ObjectPoolDependencyEntry> Dependencies;
    }
}
