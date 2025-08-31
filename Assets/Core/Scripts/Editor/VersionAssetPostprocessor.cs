#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;

namespace Core.Editor
{
    public class VersionAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            List<string> modifiedFiles = new();

            foreach (string assetPath in importedAssets)
            {
                modifiedFiles.Add(assetPath.Replace("\\", "/"));
            }

            foreach (string assetPath in deletedAssets)
            {
                modifiedFiles.Add(assetPath.Replace("\\", "/"));
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                modifiedFiles.Add(movedAssets[i].Replace("\\", "/"));
                modifiedFiles.Add(movedFromAssetPaths[i].Replace("\\", "/"));
            }

            ModVersionUpdater.ProcessVersions(modifiedFiles);
        }
    }
}

#endif
