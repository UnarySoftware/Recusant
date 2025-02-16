#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public class AssetPostprocessorCore : AssetPostprocessor
{
    public static CodeGeneration CodeGeneration = null;

    protected static void OnPostprocessAllAssets(string[] importedAssets,
        string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        CodeGeneration ??= new CodeGeneration();

        CodeGeneration.PostprocessAllAssets();
    }
}

#endif
