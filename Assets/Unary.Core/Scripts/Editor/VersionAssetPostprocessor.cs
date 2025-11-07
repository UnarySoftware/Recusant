#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unary.Core.Editor
{
    public class VersionAssetPostprocessor : AssetPostprocessor
    {
        public static Action<string[], string[], string[], string[], bool> OnPostprocessAssets;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            OnPostprocessAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths, didDomainReload);

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

            VersionUpdater.ProcessVersions(modifiedFiles);
        }
    }
}

#endif
