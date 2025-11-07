#if UNITY_EDITOR

using UnityEngine;

namespace Unary.Recusant
{
    public static class GizmosAddition
    {
        public static void DrawCapsule(Vector3 position, Quaternion rotation, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawMesh(DefaultEditorAssets.Instance.Meshes.Capsule, 0, position, rotation, Vector3.one);
        }

        public static void DrawArrow(Vector3 position, Quaternion rotation, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawMesh(DefaultEditorAssets.Instance.Meshes.Arrow, 0, position, rotation, Vector3.one);
        }
    }
}

#endif
