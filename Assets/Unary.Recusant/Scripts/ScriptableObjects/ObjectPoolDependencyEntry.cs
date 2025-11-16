using System;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [Serializable]
    public class ObjectPoolDependencyEntry
    {
        public ScriptableObjectRef<ObjectPool> DependentPool;
        [Range(1, 256)]
        public int UseCount = 1;
    }
}
