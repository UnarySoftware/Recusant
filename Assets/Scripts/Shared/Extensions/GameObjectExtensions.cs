using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExtensions
{
    /*
    public static void GetComponentsRecursive<T>(this GameObject target, List<T> result)
    {
        target.GetComponents(result);

        target.GetComponentInChildren
    }
    */

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
    /*
    public static int ToGameObjectLayer(this LayerMask mask)
    {
        int value = mask.value;
        if (value == 0)
        {
            return 0;
        }
        for (int l = 1; l < 32; l++)
        {
            if ((value & (1 << l)) != 0)
            {
                return l;
            }
        }
        return -1;
    }
    */
}

