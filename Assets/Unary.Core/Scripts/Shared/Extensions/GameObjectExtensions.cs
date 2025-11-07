using UnityEngine;

namespace Unary.Core
{
    public static class GameObjectExtensions
    {
        public static bool IsInLayerMask(this GameObject target, LayerMask mask)
        {
            return (mask.value & (1 << target.layer)) != 0;
        }
    }
}
