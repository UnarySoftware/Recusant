using Unary.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unary.Recusant
{
    [CreateAssetMenu(fileName = nameof(ObjectPool), menuName = "Recusant/Data/" + nameof(ObjectPool))]
    public class ObjectPool : BaseScriptableObject
    {
        public AssetRef<GameObject> Prefab;

        // This is done to determine if we got NetworkObject attached to a prefab
        // without instantiating this prefab just for that
        public bool PrefabNetworked = false;

        // Should this be using basic pooling or return oldest entry (useful for something like decals)
        public bool UseOldest = false;

        [Range(1, 256)]
        public int Count = 1;

        public List<ObjectPoolDependencyEntry> Dependencies;
    }
}
