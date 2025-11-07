using System.Collections.Generic;
using UnityEngine;

namespace Unary.Recusant
{
    public static class GameObjectExtensions
    {
        public static void SetLayersRecursive(this GameObject target, GameObjectLayerMask layer)
        {
            List<Transform> transforms = new();
            target.GetComponentsInChildren(true, transforms);
            foreach (var transform in transforms)
            {
                transform.gameObject.layer = (int)layer;
            }
        }
    }
}
