using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unary.Recusant
{
    [Serializable]
    public class AiTriangleData
    {
        public float Flow = -1.0f;
        public int[] Indices = null;

        public Vector3 Center = Vector3.zero;

#if UNITY_EDITOR

        public Bounds Bounds = new(Vector3.zero, Vector3.one);

        [NonSerialized]
        public HashSet<int> AllMarkups = null;

#endif

        public int[] Markups = null;
    }
}
