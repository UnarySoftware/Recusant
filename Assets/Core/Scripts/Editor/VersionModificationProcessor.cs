#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;

namespace Core.Editor
{
    public class VersionModificationProcessor : AssetModificationProcessor
    {
        public static Action<string[]> OnSaveAssets;

        public static string[] OnWillSaveAssets(string[] paths)
        {
            VersionUpdater.ProcessVersions(paths.ToList());
            OnSaveAssets(paths);
            return paths;
        }
    }
}

#endif
