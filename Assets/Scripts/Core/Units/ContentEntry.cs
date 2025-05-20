using System;

namespace Core
{
    public enum ContentEntryType
    {
        PrefabsLocal,
        PrefabsNetwork,
        Levels,
        ScriptableObjects,
        Other
    }

    [Serializable]
    public struct ContentEntrySerialized
    {
        public string Path;
        public ContentEntryType Type;
    }
}
