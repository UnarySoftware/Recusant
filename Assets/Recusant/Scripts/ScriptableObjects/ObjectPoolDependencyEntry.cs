
using System;
using UnityEngine;

namespace Recusant
{
    [Serializable]
    public class ObjectPoolDependencyEntry
    {
        public AssetRef<ObjectPool> DependentPool;
        [Range(1, 256)]
        public int UseCount = 1;
    }
}
