#if UNITY_EDITOR

using System.Linq;
using UnityEditor;

namespace Core.Editor
{
    public class FileModificationWarning : AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            VersionUpdater.ProcessVersions(paths.ToList());
            return paths;
        }
    }
}

#endif
