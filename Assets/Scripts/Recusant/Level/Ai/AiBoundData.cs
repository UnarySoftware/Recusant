using System;
using UnityEngine;

namespace Recusant
{
    [Serializable]
    public class AiBoundData
    {
        [NonSerialized]
        public static float Size = 15.0f;

#if UNITY_EDITOR

        [NonSerialized]
        public static Vector3 BoundsSize = new(Size, Size, Size);

        public Bounds Bounds;

        public int RootTriangle = -1;

#endif

        public Vector3Int Position;

        public int[] Triangles = null;

        public int[] ClosestMarkup = null;
    }
}
