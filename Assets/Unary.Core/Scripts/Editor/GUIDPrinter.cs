using UnityEngine;
using UnityEditor;

namespace Unary.Core.Editor
{
    public class GUIDPrinter : ScriptableObject
    {
        [MenuItem("Assets/Print GUID")]
        private static void PrintSelectedAssetGUID()
        {
            // Get the selected asset path
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("No asset selected.");
                return;
            }

            // Get the GUID of the asset
            string guid = AssetDatabase.AssetPathToGUID(path);
            Debug.Log($"Asset Path: {path}\nGUID: {guid}");
        }
    }
}
