using System;
using UnityEngine;

namespace Core
{
    [Serializable]
    public struct ContentManifestEntry
    {
        public string Type;
        public string Path;
    }

    public class ContentManifest : ScriptableObject
    {
        public string Name;
        public ContentManifestEntry[] Entries;
    }
}
