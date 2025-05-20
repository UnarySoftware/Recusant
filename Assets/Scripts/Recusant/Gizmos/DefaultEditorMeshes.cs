using UnityEngine;

namespace Recusant
{
    [CreateAssetMenu(fileName = "DefaultEditorMeshes", menuName = "Recusant/Editor/DefaultEditorMeshes")]
    public class DefaultEditorMeshes : ScriptableObject
    {

#if UNITY_EDITOR

        public Mesh Capsule = null;
        public Mesh Cylinder = null;
        public Mesh Cube = null;
        public Mesh Sphere = null;
        public Mesh Quad = null;
        public Mesh Arrow = null;

#endif

    }
}
