using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public static class GameObjectExtensions
    {
        public static void SetLayersRecursive(this GameObject target, int layer)
        {
            var children = target.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var child in children)
            {
                child.gameObject.layer = layer;
            }
        }

        public static bool IsInLayerMask(this GameObject target, LayerMask mask)
        {
            return (mask.value & (1 << target.layer)) != 0;
        }
    }
}
