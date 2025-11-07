#if UNITY_EDITOR

using UnityEditor;

namespace Unary.Recusant
{
    public class DefaultEditorAssets
    {
        private static DefaultEditorAssets _instance = null;

        public static DefaultEditorAssets Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new();
                    _instance.Initialize();
                }
                return _instance;
            }
            set
            {

            }
        }

        public DefaultEditorMeshes Meshes { get; private set; } = null;

        private void Initialize()
        {
            Meshes = AssetDatabase.LoadAssetAtPath<DefaultEditorMeshes>("Assets/EditorAssets/DefaultEditorMeshes.asset");
        }
    }
}

#endif
